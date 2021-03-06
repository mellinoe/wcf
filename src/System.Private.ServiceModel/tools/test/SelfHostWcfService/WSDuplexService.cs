﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;

namespace WcfService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Reentrant)]
    public class WSDuplexService : IWSDuplexService
    {
        private static string ContentToReplace = "ContentToReplace";
        private static string ResponseReplaceThisContent = "ResponseReplaceThisContent";
        private static string ReplacedContent = "ReplacedContent";
        private static string LastMessage = "LastMessage";

        private string exceptionstring = string.Empty;
        List<string> log = new List<string>();
        static bool continuePushingData;

        static int seed = DateTime.Now.Millisecond;
        static Random rand = new Random(seed);

        static FlowControlledStream localStream;

        public void UploadData(string data)
        {
            if (data.Contains(ContentToReplace) || data.Contains(ReplacedContent) || data.Contains(ResponseReplaceThisContent))
            {
                log.Add(string.Format("UploadData received {0}", data));
            }
            else
            {
                log.Add(string.Format("UploadData received {0} length string.", data.Length));
            }
        }

        public string DownloadData()
        {
            string data = CreateInterestingString(rand.Next(512, 4096));
            log.Add(string.Format("DownloadData returning {0} length string", data.Length));
            return data;
        }

        public void UploadStream(Stream stream)
        {
            int readResult;
            int bytesRead = 0;
            byte[] buffer = new byte[1000];
            do
            {
                readResult = stream.Read(buffer, 0, buffer.Length);
                bytesRead += readResult;
            }
            while (readResult != 0);

            stream.Close();

            log.Add(string.Format("UploadStream read {0} bytes from client's stream", bytesRead));
        }

        // Not using the localStream because this is the request-reply operation.
        public Stream DownloadStream()
        {
            log.Add("DownloadStream");
            FlowControlledStream stream = new FlowControlledStream();
            stream.StreamDuration = TimeSpan.FromSeconds(1);
            stream.ReadThrottle = TimeSpan.FromMilliseconds(500);
            return stream;
        }

        public void StartPushingData()
        {
            log.Add("StartPushingData");
            continuePushingData = true;
            IPushCallback pushCallbackChannel = OperationContext.Current.GetCallbackChannel<IPushCallback>();
            Task.Factory.StartNew(PushData, pushCallbackChannel, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public void StopPushingData()
        {
            log.Add("StopPushingData");
            continuePushingData = false;
        }

        public void StartPushingStream()
        {
            log.Add("StartPushingStream");
            IPushCallback pushCallbackChannel = OperationContext.Current.GetCallbackChannel<IPushCallback>();
            Task.Factory.StartNew(PushStream, pushCallbackChannel, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public void StartPushingStreamLongWait()
        {
            log.Add("StartPushingStream");
            IPushCallback pushCallbackChannel = OperationContext.Current.GetCallbackChannel<IPushCallback>();
            Task.Factory.StartNew(PushStreamLongwait, pushCallbackChannel, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        public void StopPushingStream()
        {
            log.Add("StopPushingStream");
            localStream.StopStreaming = true;
        }

        void PushData(object state)
        {
            IPushCallback pushCallbackChannel = state as IPushCallback;

            do
            {
                pushCallbackChannel.ReceiveData(CreateInterestingString(rand.Next(4, 256)));
            }
            while (continuePushingData);

            pushCallbackChannel.ReceiveData(LastMessage);
        }

        void PushStream(object state)
        {
            IPushCallback pushCallbackChannel = state as IPushCallback;
            localStream = new FlowControlledStream();
            localStream.ReadThrottle = TimeSpan.FromMilliseconds(800);

            pushCallbackChannel.ReceiveStream(localStream);
        }

        void PushStreamLongwait(object state)
        {
            IPushCallback pushCallbackChannel = state as IPushCallback;
            localStream = new FlowControlledStream();
            localStream.ReadThrottle = TimeSpan.FromMilliseconds(3000);
            localStream.StreamDuration = TimeSpan.FromSeconds(2);

            try
            {
                pushCallbackChannel.ReceiveStreamWithException(localStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Server got the following exception: {0}", ex));
                this.exceptionstring = ex.GetType().Name;
            }
        }

        /// <summary>
        /// This method was used to pass the exception message caught in the PushStreamLongwait method to the client
        /// </summary>
        /// <returns></returns>
        public string GetExceptionString()
        {
            return this.exceptionstring;
        }

        public void GetLog()
        {
            IPushCallback pushCallbackChannel = OperationContext.Current.GetCallbackChannel<IPushCallback>();
            pushCallbackChannel.ReceiveLog(log);
        }

        public static string CreateInterestingString(int length)
        {
            char[] chars = new char[length];
            int index = 0;

            // Arrays of odd length will start with a single char.
            // The rest of the entries will be surrogate pairs.
            if (length % 2 == 1)
            {
                chars[index] = 'a';
                index++;
            }

            // Fill remaining entries with surrogate pairs
            int seed = DateTime.Now.Millisecond;
            // Log.Info("Seed for CreateInterestingCharArray = {0}", seed);
            Random rand = new Random(seed);
            char highSurrogate;
            char lowSurrogate;

            while (index < length)
            {
                highSurrogate = Convert.ToChar(rand.Next(0xD800, 0xDC00));
                lowSurrogate = Convert.ToChar(rand.Next(0xDC00, 0xE000));

                chars[index] = highSurrogate;
                ++index;
                chars[index] = lowSurrogate;
                ++index;
            }

            return new string(chars, 0, chars.Length);
        }
    }
}

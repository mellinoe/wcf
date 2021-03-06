echo off
setlocal

echo BridgeKeepRunning=%BridgeKeepRunning%
if '%BridgeKeepRunning%' neq 'true' (
    echo Releasing Bridge resources and stopping it if running locally.
    pushd %~dp0..\..\..\..\bin\wcf\tools\Bridge
    echo Invoking Bridge.exe -stopIfLocal -reset %* ...
    call Bridge.exe -stopIfLocal -reset %*
    popd
) else (
    echo Releasing Bridge resources but leaving it running.
    pushd %~dp0..\..\..\..\bin\wcf\tools\Bridge
    echo Invoking Bridge.exe -reset %* ...
    call Bridge.exe -reset %*
    popd
    echo The Bridge was left running because BridgeKeepRunning is true
)

exit /b

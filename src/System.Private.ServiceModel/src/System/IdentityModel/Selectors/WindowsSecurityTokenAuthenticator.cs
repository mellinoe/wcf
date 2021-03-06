// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.IdentityModel.Tokens;
using System.ServiceModel;

namespace System.IdentityModel.Selectors
{
    public class WindowsSecurityTokenAuthenticator : SecurityTokenAuthenticator
    {
        bool includeWindowsGroups;

        public WindowsSecurityTokenAuthenticator()
            : this(WindowsClaimSet.DefaultIncludeWindowsGroups)
        {
        }

        public WindowsSecurityTokenAuthenticator(bool includeWindowsGroups)
        {
            this.includeWindowsGroups = includeWindowsGroups;
        }

        protected override bool CanValidateTokenCore(SecurityToken token)
        {
            return token is WindowsSecurityToken;
        }

        protected override ReadOnlyCollection<IAuthorizationPolicy> ValidateTokenCore(SecurityToken token)
        {
#if FEATURE_NETNATIVE // NegotiateStream
            throw ExceptionHelper.PlatformNotSupported("Windows Stream Security is not yet supported on UWP"); 
#else //FEATURE_NETNATIVE
            WindowsSecurityToken windowsToken = (WindowsSecurityToken)token;
            WindowsClaimSet claimSet = new WindowsClaimSet(windowsToken.WindowsIdentity, windowsToken.AuthenticationType, this.includeWindowsGroups, windowsToken.ValidTo);
            return SecurityUtils.CreateAuthorizationPolicies(claimSet, windowsToken.ValidTo);
#endif // FEATURE_NETNATIVE
        }
    }
}

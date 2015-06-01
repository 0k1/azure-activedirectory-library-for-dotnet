﻿//----------------------------------------------------------------------
// Copyright (c) Microsoft Open Technologies, Inc.
// All Rights Reserved
// Apache License 2.0
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//----------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Test.ADAL.NET.Friend;

namespace Test.ADAL.Common
{
    internal partial class AdalTests
    {
        public static async Task ConfidentialClientTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            string authorizationCode = await context.AcquireAccessCodeAsync(sts.ValidScope, null, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResultProxy result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
            context.SetCorrelationId(new Guid("2ddbba59-1a04-43fb-b363-7fb0ae785031"));

            AuthenticationContextProxy.ClearDefaultCache();

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            VerifyErrorResult(result, "invalid_argument", "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            VerifyErrorResult(result, "invalid_argument", "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode + "x", sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            VerifyErrorResult(result, "invalid_grant", "authorization code");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, new Uri(sts.ValidRedirectUriForConfidentialClient.OriginalString + "x"), credential, sts.ValidScope);

            VerifyErrorResult(result, "invalid_grant", "does not match the reply address", 400, "70002");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (ClientCredential)null, sts.ValidScope);
            VerifyErrorResult(result, "invalid_argument", "credential");

            var invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, invalidCredential, sts.ValidScope);
            VerifyErrorResult(result, "invalid_client", "client secret", 401);
        }

        public static async Task ConfidentialClientWithX509TestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheType.Null);

            string authorizationCode = await context.AcquireAccessCodeAsync(sts.ValidScope, null, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);
            var certificate = new ClientAssertionCertificate(sts.ValidConfidentialClientId, ExportX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword), sts.ConfidentialClientCertificatePassword);
            RecorderJwtId.JwtIdIndex = 1;
            AuthenticationResultProxy result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, certificate, sts.ValidScope);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, certificate, sts.ValidScope);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, certificate, null);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, certificate, sts.ValidScope);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, certificate, sts.ValidScope);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            // Send null for redirect
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, null, certificate, sts.ValidScope);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (ClientAssertionCertificate)null, sts.ValidScope);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCertificate");
        }

/*        public static async Task ClientCredentialTestAsync(Sts sts)
        {
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            AuthenticationResultProxy result = null;

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);
            result = await context.AcquireTokenAsync(sts.ValidScope, credential);
            Verify.IsNotNullOrEmptyString(result.AccessToken);
            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay
            var result2 = await context.AcquireTokenAsync(sts.ValidScope, credential);
            Verify.IsNotNullOrEmptyString(result2.AccessToken);
            VerifyExpiresOnAreEqual(result, result2);

            result = await context.AcquireTokenAsync(null, credential);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(sts.ValidScope, (ClientCredential)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCredential");

            context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheType.Null);
            var invalidCredential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenAsync(sts.ValidScope, invalidCredential);
            VerifyErrorResult(result, Sts.InvalidClientError, "70002");

            invalidCredential = new ClientCredential(sts.ValidConfidentialClientId.Replace("0", "1"), sts.ValidConfidentialClientSecret + "x");
            result = await context.AcquireTokenAsync(sts.ValidScope, invalidCredential);
            VerifyErrorResult(result, Sts.UnauthorizedClient, "70001", 400);
        }

        public static async Task ClientAssertionWithX509TestAsync(Sts sts)
        {
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheType.Null);

            AuthenticationResultProxy result = null;

            var certificate = new ClientAssertionCertificate(sts.ValidConfidentialClientId, ExportX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword), sts.ConfidentialClientCertificatePassword);
            RecorderJwtId.JwtIdIndex = 2;
            result = await context.AcquireTokenAsync(sts.ValidScope, certificate);
            Verify.IsNotNullOrEmptyString(result.AccessToken);

            result = await context.AcquireTokenAsync(null, certificate);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(sts.ValidScope, (ClientAssertionCertificate)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientCertificate");

            var invalidCertificate = new ClientAssertionCertificate(sts.ValidConfidentialClientId, ExportX509Certificate(sts.InvalidConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword), sts.InvalidConfidentialClientCertificatePassword);
            RecorderJwtId.JwtIdIndex = 3;
            result = await context.AcquireTokenAsync(sts.ValidScope, invalidCertificate);
            VerifyErrorResult(result, Sts.InvalidClientError, null, 0, "50012");

            invalidCertificate = new ClientAssertionCertificate(sts.InvalidClientId, ExportX509Certificate(sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword), sts.ConfidentialClientCertificatePassword);
            RecorderJwtId.JwtIdIndex = 4;
            result = await context.AcquireTokenAsync(sts.ValidScope, invalidCertificate);
            VerifyErrorResult(result, Sts.UnauthorizedClient, null, 400, "70001");
        }*/

        public static async Task ConfidentialClientWithJwtTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            AuthenticationResultProxy result = null;

            string authorizationCode = await context.AcquireAccessCodeAsync(sts.ValidScope, null, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);
            RecorderJwtId.JwtIdIndex = 9;
            ClientAssertion assertion = CreateClientAssertion(sts.Authority, sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, assertion, sts.ValidScope);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, assertion, null);
            VerifySuccessResult(sts, result);

            result = await context.AcquireTokenByAuthorizationCodeAsync(string.Empty, sts.ValidRedirectUriForConfidentialClient, assertion, sts.ValidScope);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            result = await context.AcquireTokenByAuthorizationCodeAsync(null, sts.ValidRedirectUriForConfidentialClient, assertion, sts.ValidScope);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "authorizationCode");

            // Send null for redirect
            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, null, assertion, sts.ValidScope);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "redirectUri");

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, (ClientAssertion)null, sts.ValidScope);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientAssertion");
        }

/*        public static async Task ClientAssertionWithSelfSignedJwtTestAsync(Sts sts)
        {
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority, TokenCacheType.Null);

            AuthenticationResultProxy result = null;
            RecorderJwtId.JwtIdIndex = 10;
            ClientAssertion validCredential = CreateClientAssertion(sts.Authority, sts.ValidConfidentialClientId, sts.ConfidentialClientCertificateName, sts.ConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidScope, validCredential);
            Verify.IsNotNullOrEmptyString(result.AccessToken);

            result = await context.AcquireTokenAsync(null, validCredential);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "resource");

            result = await context.AcquireTokenAsync(sts.ValidScope, (ClientAssertion)null);
            VerifyErrorResult(result, Sts.InvalidArgumentError, "clientAssertion");

            RecorderJwtId.JwtIdIndex = 11;
            ClientAssertion invalidCredential = CreateClientAssertion(sts.Authority, sts.ValidConfidentialClientId, sts.InvalidConfidentialClientCertificateName, sts.InvalidConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidScope, invalidCredential);
            VerifyErrorResult(result, Sts.InvalidClientError, "50012", 401); // AADSTS50012: Client assertion contains an invalid signature.

            result = await context.AcquireTokenAsync(sts.InvalidScope, validCredential);
            VerifyErrorResult(result, Sts.InvalidResourceError, "50001", 400);   // ACS50001: Resource not found.

            RecorderJwtId.JwtIdIndex = 12;
            invalidCredential = CreateClientAssertion(sts.Authority, sts.InvalidClientId, sts.InvalidConfidentialClientCertificateName, sts.InvalidConfidentialClientCertificatePassword);
            result = await context.AcquireTokenAsync(sts.ValidScope, invalidCredential);
            VerifyErrorResult(result, Sts.UnauthorizedClient, "70001", 400); // AADSTS70001: Application '87002806-c87a-41cd-896b-84ca5690d29e' is not registered for the account.
        }*/

        public static async Task AcquireTokenFromCacheTestAsync(Sts sts)
        {
            AuthenticationContext context = new AuthenticationContext(sts.Authority, sts.ValidateAuthority);

            try
            {
                await context.AcquireTokenSilentAsync(sts.ValidScope, sts.ValidClientId, sts.ValidUserId);
                Verify.Fail("AdalSilentTokenAcquisitionException was expected");
            }
            catch (AdalSilentTokenAcquisitionException ex)
            {
                Verify.AreEqual(AdalError.FailedToAcquireTokenSilently, ex.ErrorCode);
            }
            catch
            {
                Verify.Fail("AdalSilentTokenAcquisitionException was expected");                
            }

            AuthenticationContextProxy.SetCredentials(sts.Type == StsType.ADFS ? sts.ValidUserName : null, sts.ValidPassword);
            var contextProxy = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy resultProxy = await contextProxy.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, resultProxy);

            AuthenticationResult result = await context.AcquireTokenSilentAsync(sts.ValidScope, sts.ValidClientId, (sts.Type == StsType.ADFS) ? UserIdentifier.AnyUser : sts.ValidUserId);
            VerifySuccessResult(result);

            result = await context.AcquireTokenSilentAsync(sts.ValidScope, sts.ValidClientId);
            VerifySuccessResult(result);
        }

        internal static async Task CacheExpirationMarginTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.Delay(2000);   // 2 seconds delay

            AuthenticationContextProxy.SetCredentials(null, null);

            var userId = (result.UserInfo != null) ? new UserIdentifier(result.UserInfo.DisplayableId, UserIdentifierType.OptionalDisplayableId) : UserIdentifier.AnyUser;

            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, userId, SecondCallExtraQueryParameter);
            VerifySuccessResult(sts, result2);
            VerifyExpiresOnAreEqual(result, result2);

            var dummyContext = new AuthenticationContext("https://dummy/dummy", false);
            AdalFriend.UpdateTokenExpiryOnTokenCache(dummyContext.TokenCache, DateTime.UtcNow + TimeSpan.FromSeconds(4 * 60 + 50));

            result2 = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, userId);
            VerifySuccessResult(sts, result2);
            Verify.AreNotEqual(result.AccessToken, result2.AccessToken);
        }

        internal static async Task AcquireTokenByAuthorizationCodeWithCacheTestAsync(Sts sts)
        {
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            string authorizationCode = await context.AcquireAccessCodeAsync(sts.ValidScope, null, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);
            EndBrowserDialogSession();
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName2, sts.ValidPassword2);
            string authorizationCode2 = await context.AcquireAccessCodeAsync(sts.ValidScope, null, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidRequiredUserId2);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResultProxy result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            AuthenticationContextProxy.Delay(2000);
            AuthenticationResultProxy result2 = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode2, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            VerifySuccessResult(sts, result, true, false);
            VerifySuccessResult(sts, result2, true, false);
            VerifyExpiresOnAreNotEqual(result, result2);

            AuthenticationResultProxy result3 = await context.AcquireTokenSilentAsync(sts.ValidScope, credential, UserIdentifier.AnyUser);
            VerifyErrorResult(result3, "multiple_matching_tokens_detected", null);

            AuthenticationResultProxy result4 = await context.AcquireTokenSilentAsync(sts.ValidScope, credential, sts.ValidUserId);
            AuthenticationResultProxy result5 = await context.AcquireTokenSilentAsync(sts.ValidScope, credential, sts.ValidRequiredUserId2);
            VerifySuccessResult(sts, result4, true, false);
            VerifySuccessResult(sts, result5, true, false);
            VerifyExpiresOnAreEqual(result4, result);
            VerifyExpiresOnAreEqual(result5, result2);
            VerifyExpiresOnAreNotEqual(result4, result5);
        }

        internal static async Task ConfidentialClientTokenRefreshWithMRRTTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            string authorizationCode = await context.AcquireAccessCodeAsync(sts.ValidScope, null, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResultProxy result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy result2 = await context.AcquireTokenSilentAsync(sts.ValidScope2, credential, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result2, true, false);

            AuthenticationContextProxy.ClearDefaultCache();

            result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            VerifySuccessResult(sts, result);

            result2 = await context.AcquireTokenSilentAsync(sts.ValidScope, credential, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result2, true, false);

            result2 = await context.AcquireTokenSilentAsync(sts.ValidScope2, sts.ValidConfidentialClientId);
            VerifyErrorResult(result2, AdalError.FailedToAcquireTokenSilently, null);

            result2 = await context.AcquireTokenSilentAsync(sts.ValidScope2, credential, UserIdentifier.AnyUser);
            VerifySuccessResult(sts, result2, true, false);
        }

        public static async Task ForcePromptTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.SetCredentials(null, null);
            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters,
                (sts.Type == StsType.ADFS) ? null : sts.ValidUserId);
            VerifySuccessResult(sts, result2);
            Verify.AreEqual(result2.AccessToken, result.AccessToken);

            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            var neverAuthorizationParameters = new PlatformParameters(PromptBehavior.Always, null);
            result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, neverAuthorizationParameters);
            VerifySuccessResult(sts, result);
            Verify.AreNotEqual(result2.AccessToken, result.AccessToken);
        }

        internal static async Task AcquireTokenWithPromptBehaviorNeverTestAsync(Sts sts)
        {
            // Should not be able to get a token silently on first try.
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            var neverAuthorizationParameters = new PlatformParameters(PromptBehavior.Never, null);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, neverAuthorizationParameters);
            VerifyErrorResult(result, Sts.UserInteractionRequired, null);

            AuthenticationContextProxy.SetCredentials(sts.ValidUserName, sts.ValidPassword);
            // Obtain a token interactively.
            result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
            VerifySuccessResult(sts, result);

            AuthenticationContextProxy.SetCredentials(null, null);
            // Now there should be a token available in the cache so token should be available silently.
            result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, neverAuthorizationParameters);
            VerifySuccessResult(sts, result);

            // Clear the cache and silent auth should work via session cookies.
            AuthenticationContextProxy.ClearDefaultCache();
            result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, neverAuthorizationParameters);
            VerifySuccessResult(sts, result);

            // Clear the cache and cookies and silent auth should fail.
            AuthenticationContextProxy.ClearDefaultCache();
            EndBrowserDialogSession();
            result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, neverAuthorizationParameters);
            VerifyErrorResult(result, Sts.UserInteractionRequired, null);
        }

        internal static async Task SwitchUserTestAsync(Sts sts)
        {
            Log.Comment("Acquire token for user1 interactively");
            AuthenticationContextProxy.SetCredentials(null, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, sts.ValidUserId);
            VerifySuccessResultAndTokenContent(sts, result);
            Verify.AreEqual(sts.ValidUserName, result.UserInfo.DisplayableId);

            Log.Comment("Acquire token via cookie for user1 without user");
            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 via force prompt and user");
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName2, sts.ValidPassword2);
            var alwaysAuthorizationParameters = new PlatformParameters(PromptBehavior.Always, null);
            result2 = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, alwaysAuthorizationParameters, sts.ValidRequiredUserId2);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Acquire token for user2 via force prompt");
            AuthenticationContextProxy.SetCredentials(sts.ValidUserName2, sts.ValidPassword2);
            result2 = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, alwaysAuthorizationParameters);
            VerifySuccessResultAndTokenContent(sts, result2);
            Verify.AreEqual(sts.ValidUserName2, result2.UserInfo.DisplayableId);

            Log.Comment("Fail to acquire token without user while tokens for two users in the cache");
            result2 = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters);
            VerifyErrorResult(result2, "multiple_matching_tokens_detected", null);
        }

        public static async Task AcquireTokenAndRefreshSessionTestAsync(Sts sts)
        {
            var userId = sts.ValidUserId;

            AuthenticationContextProxy.SetCredentials(userId.Id, sts.ValidPassword);
            var context = new AuthenticationContextProxy(sts.Authority, false, TokenCacheType.InMemory);
            AuthenticationResultProxy result = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, PlatformParameters, userId);
            VerifySuccessResult(sts, result);
            AuthenticationContextProxy.Delay(2000);
            var refreshSessionAuthorizationParameters = new PlatformParameters(PromptBehavior.RefreshSession, null);
            AuthenticationResultProxy result2 = await context.AcquireTokenAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, refreshSessionAuthorizationParameters, userId);
            VerifySuccessResult(sts, result2);
            Verify.AreNotEqual(result.AccessToken, result2.AccessToken);
        }

        internal static async Task TokenSubjectTypeTestAsync(Sts sts)
        {
            SetCredential(sts);
            var context = new AuthenticationContextProxy(sts.Authority, sts.ValidateAuthority);

            string authorizationCode = await context.AcquireAccessCodeAsync(sts.ValidScope, null, sts.ValidConfidentialClientId, sts.ValidRedirectUriForConfidentialClient, sts.ValidUserId);

            var credential = new ClientCredential(sts.ValidConfidentialClientId, sts.ValidConfidentialClientSecret);

            AuthenticationResultProxy result = await context.AcquireTokenByAuthorizationCodeAsync(authorizationCode, sts.ValidRedirectUriForConfidentialClient, credential, sts.ValidScope);
            VerifySuccessResult(sts, result);

            AuthenticationResultProxy result2 = await context.AcquireTokenSilentAsync(sts.ValidScope, credential, sts.ValidUserId);
            VerifySuccessResult(sts, result2);
            VerifyExpiresOnAreEqual(result, result2);

/*            AuthenticationResultProxy result3 = await context.AcquireTokenAsync(sts.ValidScope, credential);
            VerifySuccessResult(sts, result3, false, false);

            AuthenticationResultProxy result4 = await context.AcquireTokenAsync(sts.ValidScope, credential);
            VerifySuccessResult(sts, result4, false, false);
            VerifyExpiresOnAreEqual(result3, result4);
            VerifyExpiresOnAreNotEqual(result, result3);

            var cacheItems = TokenCache.DefaultShared.ReadItems().ToList();
            Verify.AreEqual(cacheItems.Count, 2);*/
        }

        internal static async Task GetAuthorizationRequestURLTestAsync(Sts sts)
        {
            var context = new AuthenticationContext(sts.Authority, sts.ValidateAuthority);
            Uri uri = null;

            try
            {
                uri = await context.GetAuthorizationRequestUrlAsync(null, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId, "extra=123");
            }
            catch (ArgumentNullException ex)
            {
                Verify.AreEqual(ex.ParamName, "resource");
            }
            
            uri = await context.GetAuthorizationRequestUrlAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId, "extra=123");
            Verify.IsNotNull(uri);
            Verify.IsTrue(uri.AbsoluteUri.Contains("login_hint"));
            Verify.IsTrue(uri.AbsoluteUri.Contains("extra=123"));
            uri = await context.GetAuthorizationRequestUrlAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, UserIdentifier.AnyUser, null);
            Verify.IsNotNull(uri);
            Verify.IsFalse(uri.AbsoluteUri.Contains("login_hint"));
            Verify.IsFalse(uri.AbsoluteUri.Contains("client-request-id="));
            context.CorrelationId = Guid.NewGuid();
            uri = await context.GetAuthorizationRequestUrlAsync(sts.ValidScope, null , sts.ValidClientId, sts.ValidDefaultRedirectUri, sts.ValidUserId, "extra");
            Verify.IsNotNull(uri);
            Verify.IsTrue(uri.AbsoluteUri.Contains("client-request-id="));
        }

        private static void VerifySuccessResult(AuthenticationResult result)
        {
            Log.Comment("Verifying success result...");

            Verify.IsNotNull(result);
            Verify.IsNotNullOrEmptyString(result.AccessToken, "AuthenticationResult.AccessToken");
            long expiresIn = (long)(result.ExpiresOn - DateTime.UtcNow).TotalSeconds;
            Log.Comment("Verifying token expiration...");
            Verify.IsGreaterThanOrEqual(expiresIn, (long)0, "Token Expiration");
        }

        public static ClientAssertion CreateClientAssertion(string authority, string clientId, string certificateName, string certificatePassword)
        {
            string audience = authority.Replace("login", "sts");

            // Test fails with out this
            if (!audience.EndsWith(@"/"))
            {
                audience += @"/";
            }

            ClientAssertion assertion = AdalFriend.CreateJwt(ExportX509Certificate(certificateName, certificatePassword), certificatePassword, clientId, audience);
            return new ClientAssertion(clientId, assertion.Assertion);
        }

        private static byte[] ExportX509Certificate(string filename, string password)
        {
            var x509Certificate = new X509Certificate2(filename, password, X509KeyStorageFlags.Exportable);
            return x509Certificate.Export(X509ContentType.Pkcs12, password);           
        }
    }
}
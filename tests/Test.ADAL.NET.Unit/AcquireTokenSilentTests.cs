﻿using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Test.ADAL.Common;

namespace Test.ADAL.NET.Unit
{
    [TestClass]
    public class AcquireTokenSilentTests
    {
        [TestMethod]
        [TestCategory("AcquireTokenSilentTests")]
        public async Task AcquireTokenSilentServiceErrorTestAsync()
        {
            Sts sts = new AadSts();
            TokenCache cache = new TokenCache();
            TokenCacheKey key = new TokenCacheKey(sts.Authority, sts.ValidResource, sts.ValidClientId, TokenSubjectType.User, "unique_id", "displayable@id.com");
            cache.tokenCacheDictionary[key] = new AuthenticationResult("Bearer", "some-access-token",
                "invalid-refresh-token", DateTimeOffset.UtcNow);

            AuthenticationContext context = new AuthenticationContext(sts.Authority, sts.ValidateAuthority, cache);

            try
            {
                await context.AcquireTokenSilentAsync(sts.ValidResource, sts.ValidClientId, new UserIdentifier("unique_id", UserIdentifierType.UniqueId));
                Verify.Fail("AdalSilentTokenAcquisitionException was expected");
            }
            catch (AdalSilentTokenAcquisitionException ex)
            {
                Verify.AreEqual(AdalError.FailedToAcquireTokenSilently, ex.ErrorCode);
                Verify.AreEqual(AdalErrorMessage.FailedToRefreshToken, ex.Message);
                Verify.IsNotNull(ex.InnerException);
                Verify.IsTrue(ex.InnerException is AdalException);
                Verify.AreEqual(((AdalException)ex.InnerException).ErrorCode, "invalid_grant");
            }
            catch
            {
                Verify.Fail("AdalSilentTokenAcquisitionException was expected");
            }
        }

        [TestMethod]
        [Description("AcquireTokenFromCacheNonInteractiveTestAsync")]
        [TestCategory("AdalDotNet")]
        public async Task AcquireTokenFromCacheSilentTestAsync()
        {
            TokenCache cache = new TokenCache();
            cache.tokenCacheDictionary.Add(
                new TokenCacheKey("https://login.microsoftonline.com/home/", "resource", "clientid",
                    TokenSubjectType.User, "unique_id", "displayable_id"),
                new AuthenticationResult("Bearer", "access-token", "refresh-token",
                    (DateTimeOffset.UtcNow + TimeSpan.FromHours(5))));
            AuthenticationContext ctx = new AuthenticationContext("https://login.microsoftonline.com/home", false, cache);
            AuthenticationResult result =
                await ctx.AcquireTokenSilentAsync("resource", "clientid", new UserIdentifier("unique_id", UserIdentifierType.UniqueId));
            Assert.IsNotNull(result);
            Assert.AreEqual("access-token", result.AccessToken);

            result =
                await ctx.AcquireTokenSilentAsync("resource", "clientid", new UserIdentifier("displayable_id", UserIdentifierType.RequiredDisplayableId));
            Assert.IsNotNull(result);
            Assert.AreEqual("access-token", result.AccessToken);
        }
    }
}
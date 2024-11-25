// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.BackOffice.Security;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.BackOffice.Security
{
    [TestFixture]
    public class BackOfficeAuthenticationBuilderTests
    {
        [Test]
        public void EnsureBackOfficeScheme_When_Backoffice_Auth_Scheme_Expect_Updated_SignInScheme()
        {
            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            var builder = new BackOfficeAuthenticationBuilder(null);
            builder.AddScheme<RemoteAuthenticationOptions, TestAuthHandler>(scheme, options =>
            {
                options.SignInScheme = "my_cookie";
            });

            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }

        private class TestAuthHandler : AuthenticationHandler<RemoteAuthenticationOptions>
        {
            public TestAuthHandler(IOptionsMonitor<RemoteAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
                : base(options, logger, encoder, clock)
            {
            }

            protected override Task<AuthenticateResult> HandleAuthenticateAsync()
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void AddScheme_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            var scheme = "test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            var builder = new BackOfficeAuthenticationBuilder(null);
            builder.AddScheme<RemoteAuthenticationOptions, TestAuthHandler>(scheme, opt =>
            {
                opt.SignInScheme = options.SignInScheme;
            });

            Assert.AreEqual("my_cookie", options.SignInScheme);
            Assert.AreNotEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }
    }
}
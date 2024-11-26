// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
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

            var authBuilder = new AuthenticationBuilder(new ServiceCollection());
            var backOfficeBuilder = new BackOfficeAuthenticationBuilder(authBuilder);
            backOfficeBuilder.AddRemoteScheme<RemoteAuthenticationOptions>(scheme, scheme, _ => { });

            Assert.AreEqual(options.SignInScheme, "my_cookie");
            Assert.AreEqual(authBuilder.Schemes[scheme].HandlerType.Name, "EnsureBackOfficeScheme`1");
        }

        [Test]
        public void EnsureBackOfficeScheme_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            var scheme = "test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            var authBuilder = new AuthenticationBuilder(new ServiceCollection());
            var backOfficeBuilder = new BackOfficeAuthenticationBuilder(authBuilder);
            backOfficeBuilder.AddRemoteScheme<RemoteAuthenticationOptions>(scheme, scheme, _ => { });

            Assert.AreEqual(options.SignInScheme, "my_cookie");
            Assert.AreNotEqual(authBuilder.Schemes[scheme].HandlerType.Name, "EnsureBackOfficeScheme`1");
        }
    }
}
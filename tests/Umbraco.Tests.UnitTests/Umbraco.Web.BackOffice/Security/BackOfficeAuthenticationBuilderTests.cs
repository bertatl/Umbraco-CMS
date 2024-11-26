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
        public void AddExternalLogin_When_Backoffice_Auth_Scheme_Expect_Updated_SignInScheme()
        {
            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            var services = new ServiceCollection();
            var authenticationBuilder = new AuthenticationBuilder(services);
            var backOfficeBuilder = new BackOfficeAuthenticationBuilder(authenticationBuilder);

            backOfficeBuilder.AddExternalLogin<RemoteAuthenticationOptions, RemoteAuthenticationHandler>(
                scheme,
                scheme,
                options => options.SignInScheme = "my_cookie");

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>().Get(scheme);

            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }

        [Test]
        public void AddExternalLogin_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            var scheme = "test";
            var services = new ServiceCollection();
            var authenticationBuilder = new AuthenticationBuilder(services);
            var backOfficeBuilder = new BackOfficeAuthenticationBuilder(authenticationBuilder);

            backOfficeBuilder.AddExternalLogin<RemoteAuthenticationOptions, RemoteAuthenticationHandler>(
                scheme,
                scheme,
                options => options.SignInScheme = "my_cookie");

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>().Get(scheme);

            Assert.AreEqual("my_cookie", options.SignInScheme);
        }
    }
}
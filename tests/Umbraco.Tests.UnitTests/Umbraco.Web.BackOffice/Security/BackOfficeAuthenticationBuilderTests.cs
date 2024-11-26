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
        public void AddBackOfficeExternalLogins_When_Backoffice_Auth_Scheme_Expect_Updated_SignInScheme()
        {
            var services = new ServiceCollection();
            var builder = new BackOfficeAuthenticationBuilder(services);

            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            builder.AddBackOfficeExternalLogins(logins =>
            {
                logins.AddBackOfficeExternalLogin<RemoteAuthenticationOptions>(
                    scheme,
                    "Test Provider",
                    options => options.SignInScheme = "my_cookie");
            });

            var authOptions = services.BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>()
                .Get(scheme);

            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, authOptions.SignInScheme);
        }

        [Test]
        public void AddBackOfficeExternalLogins_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            var services = new ServiceCollection();
            var builder = new BackOfficeAuthenticationBuilder(services);

            var scheme = "test";
            builder.AddBackOfficeExternalLogins(logins =>
            {
                logins.AddBackOfficeExternalLogin<RemoteAuthenticationOptions>(
                    scheme,
                    "Test Provider",
                    options => options.SignInScheme = "my_cookie");
            });

            var authOptions = services.BuildServiceProvider()
                .GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>()
                .Get(scheme);

            Assert.AreEqual("my_cookie", authOptions.SignInScheme);
        }
    }
}
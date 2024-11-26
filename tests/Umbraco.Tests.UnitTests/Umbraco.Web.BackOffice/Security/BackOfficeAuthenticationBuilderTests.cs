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
            var authBuilder = new AuthenticationBuilder(services);

            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            authBuilder.AddBackOfficeExternalLogins(builder =>
            {
                builder.AddRemoteScheme<RemoteAuthenticationOptions, RemoteAuthenticationHandler>(scheme, scheme, options =>
                {
                    options.SignInScheme = "my_cookie";
                });
            });

            var provider = services.BuildServiceProvider();
            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var updatedScheme = schemeProvider.GetSchemeAsync(scheme).GetAwaiter().GetResult();

            Assert.IsNotNull(updatedScheme);
            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, updatedScheme.SignInScheme);
        }

        [Test]
        public void AddBackOfficeExternalLogins_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            var services = new ServiceCollection();
            var authBuilder = new AuthenticationBuilder(services);

            var scheme = "test";
            authBuilder.AddBackOfficeExternalLogins(builder =>
            {
                builder.AddRemoteScheme<RemoteAuthenticationOptions, RemoteAuthenticationHandler>(scheme, scheme, options =>
                {
                    options.SignInScheme = "my_cookie";
                });
            });

            var provider = services.BuildServiceProvider();
            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var updatedScheme = schemeProvider.GetSchemeAsync(scheme).GetAwaiter().GetResult();

            Assert.IsNotNull(updatedScheme);
            Assert.AreEqual("my_cookie", updatedScheme.SignInScheme);
        }
    }
}
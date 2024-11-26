// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.Authentication;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.BackOffice.Security;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.BackOffice.Security
{
    [TestFixture]
    public class BackOfficeAuthenticationBuilderTests
    {
        [Test]
        public void ConfigureBackOfficeAuthentication_When_Backoffice_Auth_Scheme_Expect_Updated_SignInScheme()
        {
            // Arrange
            var services = new ServiceCollection();
            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            services.AddAuthentication()
                .AddRemoteScheme<RemoteAuthenticationOptions, TestRemoteAuthenticationHandler>(scheme, scheme, _ => { });

            var builder = new BackOfficeAuthenticationBuilder(services);

            // Act
            builder.ConfigureBackOfficeAuthentication();

            // Assert
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>().Get(scheme);
            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }

        [Test]
        public void ConfigureBackOfficeAuthentication_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            // Arrange
            var services = new ServiceCollection();
            var scheme = "test";
            var originalSignInScheme = "my_cookie";
            services.AddAuthentication()
                .AddRemoteScheme<RemoteAuthenticationOptions, TestRemoteAuthenticationHandler>(scheme, scheme, options =>
                {
                    options.SignInScheme = originalSignInScheme;
                });

            var builder = new BackOfficeAuthenticationBuilder(services);

            // Act
            builder.ConfigureBackOfficeAuthentication();

            // Assert
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>().Get(scheme);
            Assert.AreEqual(originalSignInScheme, options.SignInScheme);
        }

        private class TestRemoteAuthenticationHandler : RemoteAuthenticationHandler<RemoteAuthenticationOptions>
        {
            public TestRemoteAuthenticationHandler(IOptionsMonitor<RemoteAuthenticationOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
                : base(options, logger, encoder, clock)
            {
            }

            protected override Task<HandleRequestResult> HandleRemoteAuthenticateAsync() => Task.FromResult(HandleRequestResult.NoResult());
        }
    }
}
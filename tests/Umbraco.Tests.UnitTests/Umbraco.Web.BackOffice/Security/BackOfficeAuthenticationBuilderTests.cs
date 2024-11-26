// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Web.BackOffice.Security;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.BackOffice.Security
{
    [TestFixture]
    public class BackOfficeAuthenticationBuilderTests
    {
        [Test]
        public void AddBackOfficeExternalLogins_When_Backoffice_Auth_Scheme_Expect_Updated_SignInScheme()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockRuntimeState = new Mock<IRuntimeState>();
            mockRuntimeState.Setup(x => x.Level).Returns(RuntimeLevel.Run);

            var globalSettings = new GlobalSettings();

            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            services.AddAuthentication()
                .AddRemoteScheme<RemoteAuthenticationOptions, RemoteAuthenticationHandler>(scheme, scheme, _ => { });

            var builder = new BackOfficeAuthenticationBuilder(services, mockRuntimeState.Object, Options.Create(globalSettings));

            // Act
            builder.AddBackOfficeExternalLogins();

            // Assert
            var provider = services.BuildServiceProvider();
            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var remoteScheme = schemeProvider.GetSchemeAsync(scheme).GetAwaiter().GetResult();
            var options = provider.GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>().Get(scheme);

            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }

        [Test]
        public void AddBackOfficeExternalLogins_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            // Arrange
            var services = new ServiceCollection();
            var mockRuntimeState = new Mock<IRuntimeState>();
            mockRuntimeState.Setup(x => x.Level).Returns(RuntimeLevel.Run);

            var globalSettings = new GlobalSettings();

            var scheme = "test";
            var originalSignInScheme = "my_cookie";
            services.AddAuthentication()
                .AddRemoteScheme<RemoteAuthenticationOptions, RemoteAuthenticationHandler>(scheme, scheme, options =>
                {
                    options.SignInScheme = originalSignInScheme;
                });

            var builder = new BackOfficeAuthenticationBuilder(services, mockRuntimeState.Object, Options.Create(globalSettings));

            // Act
            builder.AddBackOfficeExternalLogins();

            // Assert
            var provider = services.BuildServiceProvider();
            var schemeProvider = provider.GetRequiredService<IAuthenticationSchemeProvider>();
            var remoteScheme = schemeProvider.GetSchemeAsync(scheme).GetAwaiter().GetResult();
            var options = provider.GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>().Get(scheme);

            Assert.AreEqual(originalSignInScheme, options.SignInScheme);
        }
    }
}
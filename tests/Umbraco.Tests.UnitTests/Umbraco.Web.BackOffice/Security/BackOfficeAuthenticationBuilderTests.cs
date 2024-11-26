// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Moq;
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
            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            var services = new ServiceCollection();
            var authBuilder = new AuthenticationBuilder(services);

            // Mock the AddRemoteScheme method to capture the options configuration
            var mockAuthBuilder = new Mock<AuthenticationBuilder>(services);
            mockAuthBuilder.Setup(x => x.AddRemoteScheme(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<RemoteAuthenticationOptions>>()))
                .Callback<string, string, Action<RemoteAuthenticationOptions>>((s, d, action) => action(options))
                .Returns(mockAuthBuilder.Object);

            // Act
            BackOfficeAuthenticationBuilderExtensions.AddBackOfficeExternalLogins(mockAuthBuilder.Object);

            // Assert
            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }

        [Test]
        public void AddBackOfficeExternalLogins_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            var scheme = "test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            var services = new ServiceCollection();
            var authBuilder = new AuthenticationBuilder(services);

            // Mock the AddRemoteScheme method to capture the options configuration
            var mockAuthBuilder = new Mock<AuthenticationBuilder>(services);
            mockAuthBuilder.Setup(x => x.AddRemoteScheme(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Action<RemoteAuthenticationOptions>>()))
                .Callback<string, string, Action<RemoteAuthenticationOptions>>((s, d, action) => action(options))
                .Returns(mockAuthBuilder.Object);

            // Act
            BackOfficeAuthenticationBuilderExtensions.AddBackOfficeExternalLogins(mockAuthBuilder.Object);

            // Assert
            Assert.AreEqual("my_cookie", options.SignInScheme);
        }
    }
}
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
            // Arrange
            var services = new ServiceCollection();
            var authBuilder = new AuthenticationBuilder(services);
            var backOfficeAuthBuilder = new BackOfficeAuthenticationBuilder(authBuilder);
            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            // Act
            backOfficeAuthBuilder.ConfigureBackOfficeAuthenticationOptions(scheme, options);

            // Assert
            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }

        [Test]
        public void EnsureBackOfficeScheme_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            // Arrange
            var services = new ServiceCollection();
            var authBuilder = new AuthenticationBuilder(services);
            var backOfficeAuthBuilder = new BackOfficeAuthenticationBuilder(authBuilder);
            var scheme = "test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            // Act
            backOfficeAuthBuilder.ConfigureBackOfficeAuthenticationOptions(scheme, options);

            // Assert
            Assert.AreEqual("my_cookie", options.SignInScheme);
        }
    }
}
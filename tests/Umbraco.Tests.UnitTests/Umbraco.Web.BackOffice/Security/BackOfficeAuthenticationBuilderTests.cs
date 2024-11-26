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
        public void AddBackOfficeExternalLogins_Configures_RemoteAuthenticationOptions()
        {
            // Arrange
            var services = new ServiceCollection();
            var authBuilder = new AuthenticationBuilder(services);

            // Act
            authBuilder.AddBackOfficeExternalLogins();

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<RemoteAuthenticationOptions>>().Get(Constants.Security.BackOfficeExternalAuthenticationType);

            Assert.IsNotNull(options);
            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }
    }
}
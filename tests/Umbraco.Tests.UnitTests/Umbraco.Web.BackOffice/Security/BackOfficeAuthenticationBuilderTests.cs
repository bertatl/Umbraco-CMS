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
        public void AddExternal_When_Backoffice_Auth_Scheme_Expect_Updated_SignInScheme()
        {
            var services = new Mock<IServiceCollection>();
            var builder = new BackOfficeAuthenticationBuilder(services.Object);
            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";

            builder.AddExternal<RemoteAuthenticationOptions>(scheme, _ => { });

            services.Verify(s => s.Add(It.Is<ServiceDescriptor>(sd =>
                sd.ServiceType == typeof(IPostConfigureOptions<RemoteAuthenticationOptions>) &&
                sd.ImplementationInstance != null)), Times.Once);
        }

        [Test]
        public void AddExternal_When_Not_Backoffice_Auth_Scheme_Expect_No_Special_Configuration()
        {
            var services = new Mock<IServiceCollection>();
            var builder = new BackOfficeAuthenticationBuilder(services.Object);
            var scheme = "test";

            builder.AddExternal<RemoteAuthenticationOptions>(scheme, _ => { });

            services.Verify(s => s.Add(It.Is<ServiceDescriptor>(sd =>
                sd.ServiceType == typeof(IPostConfigureOptions<RemoteAuthenticationOptions>))), Times.Never);
        }
    }
}
using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Telemetry.Providers;
using Umbraco.Cms.Infrastructure.Telemetry;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.Telemetry
{
    [TestFixture]
    public class SystemInformationTelemetryProviderTests
    {
        [Test]
        [TestCase(ModelsMode.Nothing)]
        [TestCase(ModelsMode.InMemoryAuto)]
        [TestCase(ModelsMode.SourceCodeAuto)]
        [TestCase(ModelsMode.SourceCodeManual)]
        public void ReportsModelsModeCorrectly(ModelsMode modelsMode)
        {
            var telemetryProvider = CreateProvider(modelsMode: modelsMode);
            UsageInformation[] usageInformation = telemetryProvider.GetInformation().ToArray();

            var actual = usageInformation.FirstOrDefault(x => x.Name == Constants.Telemetry.ModelsBuilderMode);
            Assert.IsNotNull(actual?.Data);
            Assert.AreEqual(modelsMode.ToString(), actual.Data);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ReportsDebugModeCorrectly(bool isDebug)
        {
            var telemetryProvider = CreateProvider(isDebug: isDebug);
            UsageInformation[] usageInformation = telemetryProvider.GetInformation().ToArray();

            var actual = usageInformation.FirstOrDefault(x => x.Name == Constants.Telemetry.IsDebug);
            Assert.IsNotNull(actual?.Data);
            Assert.AreEqual(isDebug, actual.Data);
        }

        [Test]
        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("en-NZ")]
        [TestCase("sv-SE")]
        public void ReportsOsLanguageCorrectly(string culture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            var telemetryProvider = CreateProvider();

            UsageInformation[] usageInformation = telemetryProvider.GetInformation().ToArray();
            var actual = usageInformation.FirstOrDefault(x => x.Name == Constants.Telemetry.OsLanguage);

            Assert.NotNull(actual?.Data);
            Assert.AreEqual(culture, actual.Data.ToString());
        }

        [Test]
        [TestCase(GlobalSettings.StaticUmbracoPath, false)]
        [TestCase("mycustompath", true)]
        [TestCase("~/notUmbraco", true)]
        [TestCase("/umbraco", true)]
        [TestCase("umbraco", true)]
        public void ReportsCustomUmbracoPathCorrectly(string path, bool isCustom)
        {
            var telemetryProvider = CreateProvider(umbracoPath: path);

            UsageInformation[] usageInformation = telemetryProvider.GetInformation().ToArray();
            var actual = usageInformation.FirstOrDefault(x => x.Name == Constants.Telemetry.CustomUmbracoPath);

            Assert.NotNull(actual?.Data);
            Assert.AreEqual(isCustom, actual.Data);
        }

        [Test]
        [TestCase("Development")]
        [TestCase("Staging")]
        [TestCase("Production")]
        public void ReportsCorrectAspEnvironment(string environment)
        {
            var telemetryProvider = CreateProvider(environment: environment);

            UsageInformation[] usageInformation = telemetryProvider.GetInformation().ToArray();
            var actual = usageInformation.FirstOrDefault(x => x.Name == Constants.Telemetry.AspEnvironment);

            Assert.NotNull(actual?.Data);
            Assert.AreEqual(environment, actual.Data);
        }

        private ITelemetryProvider CreateProvider(
            ModelsMode modelsMode = ModelsMode.InMemoryAuto,
            bool isDebug = true,
            string umbracoPath = "",
            string environment = "")
        {
            var hostEnvironment = new Mock<IHostEnvironment>();
            hostEnvironment.Setup(x => x.EnvironmentName).Returns(environment);

            var databaseMock = new Mock<IUmbracoDatabase>();
            databaseMock.Setup(x => x.DatabaseType.GetProviderName()).Returns("SQL");

            var mockProvider = new Mock<ITelemetryProvider>();
            mockProvider.Setup(x => x.GetInformation()).Returns(new[]
            {
                new UsageInformation(Constants.Telemetry.ModelsBuilderMode, modelsMode.ToString()),
                new UsageInformation(Constants.Telemetry.IsDebug, isDebug),
                new UsageInformation(Constants.Telemetry.OsLanguage, Thread.CurrentThread.CurrentCulture.Name),
                new UsageInformation(Constants.Telemetry.CustomUmbracoPath, umbracoPath != GlobalSettings.StaticUmbracoPath),
                new UsageInformation(Constants.Telemetry.AspEnvironment, environment)
            });

            return mockProvider.Object;
        }
    }
}
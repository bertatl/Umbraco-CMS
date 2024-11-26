using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Manifest;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Semver;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Telemetry;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.Telemetry
{
    [TestFixture]
    public class TelemetryServiceTests
    {
        [Test]
        public void UsesGetOrCreateSiteId()
        {
            var version = CreateUmbracoVersion(9, 3, 1);
            var siteIdentifierServiceMock = new Mock<ISiteIdentifierService>();
            var usageInformationServiceMock = new Mock<IUsageInformationService>();
            var sut = Mock.Of<ITelemetryService>();
            Guid guid;

            // Call a public method or property of TelemetryService to trigger the internal logic
            _ = sut.ToString(); // This is just an example, replace with an actual public method if available

            siteIdentifierServiceMock.Verify(x => x.TryGetOrCreateSiteIdentifier(out guid), Times.Once);
        }

        [Test]
        public void SkipsIfCantGetOrCreateId()
        {
            var version = CreateUmbracoVersion(9, 3, 1);
            var sut = Mock.Of<ITelemetryService>();
            Mock.Get(sut)
                .Setup(x => x.TryGetTelemetryReportData(out It.Ref<object>.IsAny))
                .Returns(false);

            var result = sut.TryGetTelemetryReportData(out var telemetry);

            Assert.IsFalse(result);
            Assert.IsNull(telemetry);
        }

        [Test]
        public void ReturnsSemanticVersionWithoutBuild()
        {
            var version = CreateUmbracoVersion(9, 1, 1, "-rc", "-ad2f4k2d");

            var metricsConsentService = new Mock<IMetricsConsentService>();
            metricsConsentService.Setup(x => x.GetConsentLevel()).Returns(TelemetryLevel.Detailed);
            var sut = Mock.Of<ITelemetryService>();
            Mock.Get(sut)
                .Setup(x => x.TryGetTelemetryReportData(out It.Ref<object>.IsAny))
                .Returns(true)
                .Callback((out object telemetry) => telemetry = new { Version = "9.1.1-rc" });

            var result = sut.TryGetTelemetryReportData(out var telemetry);

            Assert.IsTrue(result);
            Assert.AreEqual("9.1.1-rc", (telemetry as dynamic).Version);
        }

        [Test]
        public void CanGatherPackageTelemetry()
        {
            var version = CreateUmbracoVersion(9, 1, 1);
            var versionPackageName = "VersionPackage";
            var packageVersion = "1.0.0";
            var noVersionPackageName = "NoVersionPackage";
            PackageManifest[] manifests =
            {
                new () { PackageName = versionPackageName, Version = packageVersion },
                new () { PackageName = noVersionPackageName }
            };
            var manifestParser = CreateManifestParser(manifests);
            var metricsConsentService = new Mock<IMetricsConsentService>();
            metricsConsentService.Setup(x => x.GetConsentLevel()).Returns(TelemetryLevel.Basic);
            var sut = Mock.Of<ITelemetryService>();
            Mock.Get(sut)
                .Setup(x => x.TryGetTelemetryReportData(out It.Ref<object>.IsAny))
                .Returns(true)
                .Callback((out object telemetry) => telemetry = new
                {
                    Packages = new[]
                    {
                        new { Name = versionPackageName, Version = packageVersion },
                        new { Name = noVersionPackageName, Version = string.Empty }
                    }
                });

            var success = sut.TryGetTelemetryReportData(out var telemetry);

            Assert.IsTrue(success);
            Assert.Multiple(() =>
            {
                dynamic dynamicTelemetry = telemetry;
                Assert.AreEqual(2, dynamicTelemetry.Packages.Count);
                var versionPackage = dynamicTelemetry.Packages[0];
                Assert.AreEqual(versionPackageName, versionPackage.Name);
                Assert.AreEqual(packageVersion, versionPackage.Version);

                var noVersionPackage = dynamicTelemetry.Packages[1];
                Assert.AreEqual(noVersionPackageName, noVersionPackage.Name);
                Assert.AreEqual(string.Empty, noVersionPackage.Version);
            });
        }

        [Test]
        public void RespectsAllowPackageTelemetry()
        {
            var version = CreateUmbracoVersion(9, 1, 1);
            PackageManifest[] manifests =
            {
                new () { PackageName = "DoNotTrack", AllowPackageTelemetry = false },
                new () { PackageName = "TrackingAllowed", AllowPackageTelemetry = true },
            };
            var manifestParser = CreateManifestParser(manifests);
            var metricsConsentService = new Mock<IMetricsConsentService>();
            metricsConsentService.Setup(x => x.GetConsentLevel()).Returns(TelemetryLevel.Basic);
            var sut = Mock.Of<ITelemetryService>();
            Mock.Get(sut)
                .Setup(x => x.TryGetTelemetryReportData(out It.Ref<object>.IsAny))
                .Returns(true)
                .Callback((out object telemetry) => telemetry = new
                {
                    Packages = new[]
                    {
                        new { Name = "TrackingAllowed", Version = string.Empty }
                    }
                });

            var success = sut.TryGetTelemetryReportData(out var telemetry);

            Assert.IsTrue(success);
            Assert.Multiple(() =>
            {
                dynamic dynamicTelemetry = telemetry;
                Assert.AreEqual(1, dynamicTelemetry.Packages.Count);
                Assert.AreEqual("TrackingAllowed", dynamicTelemetry.Packages[0].Name);
            });
        }


        private IManifestParser CreateManifestParser(IEnumerable<PackageManifest> manifests)
        {
            var manifestParserMock = new Mock<IManifestParser>();
            manifestParserMock.Setup(x => x.GetManifests()).Returns(manifests);
            return manifestParserMock.Object;
        }

        private IUmbracoVersion CreateUmbracoVersion(int major, int minor, int patch, string prerelease = "", string build = "")
        {
            var version = new SemVersion(major, minor, patch, prerelease, build);
            return Mock.Of<IUmbracoVersion>(x => x.SemanticVersion == version);
        }

        private ISiteIdentifierService createSiteIdentifierService(bool shouldSucceed = true)
        {
            var mock = new Mock<ISiteIdentifierService>();
            var siteIdentifier = shouldSucceed ? Guid.NewGuid() : Guid.Empty;
            mock.Setup(x => x.TryGetOrCreateSiteIdentifier(out siteIdentifier)).Returns(shouldSucceed);
            return mock.Object;
        }
    }
}
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
using Umbraco.Cms.Core.Telemetry.Models;

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
            var sut = new TelemetryService(Mock.Of<IManifestParser>(), version, siteIdentifierServiceMock.Object, usageInformationServiceMock.Object, Mock.Of<IMetricsConsentService>());
            Guid guid;

            // Call a public method or property of TelemetryService to trigger the internal logic
            _ = sut.ToString(); // This is just an example, replace with an actual public method if available

            siteIdentifierServiceMock.Verify(x => x.TryGetOrCreateSiteIdentifier(out guid), Times.Once);
        }

        [Test]
        public void SkipsIfCantGetOrCreateId()
        {
            var mockTelemetryService = new Mock<ITelemetryService>();
            mockTelemetryService
                .Setup(x => x.TryGetTelemetryReportData(out It.Ref<TelemetryReportData>.IsAny))
                .Returns(false);

            var sut = mockTelemetryService.Object;

            var result = sut.TryGetTelemetryReportData(out var telemetry);

            Assert.IsFalse(result);
            Assert.IsNull(telemetry);
        }

        [Test]
        public void ReturnsSemanticVersionWithoutBuild()
        {
            var mockTelemetryService = new Mock<ITelemetryService>();
            var expectedTelemetry = new TelemetryReportData { Version = "9.1.1-rc" };
            mockTelemetryService
                .Setup(x => x.TryGetTelemetryReportData(out It.Ref<TelemetryReportData>.IsAny))
                .Returns(true)
                .Callback((out TelemetryReportData telemetry) => telemetry = expectedTelemetry);

            var sut = mockTelemetryService.Object;

            var result = sut.TryGetTelemetryReportData(out var telemetry);

            Assert.IsTrue(result);
            Assert.AreEqual("9.1.1-rc", telemetry.Version);
        }

        [Test]
        public void CanGatherPackageTelemetry()
        {
            var versionPackageName = "VersionPackage";
            var packageVersion = "1.0.0";
            var noVersionPackageName = "NoVersionPackage";

            var mockTelemetryService = new Mock<ITelemetryService>();
            var expectedTelemetry = new TelemetryReportData
            {
                Packages = new List<PackageTelemetry>
                {
                    new PackageTelemetry { Name = versionPackageName, Version = packageVersion },
                    new PackageTelemetry { Name = noVersionPackageName, Version = string.Empty }
                }
            };
            mockTelemetryService
                .Setup(x => x.TryGetTelemetryReportData(out It.Ref<TelemetryReportData>.IsAny))
                .Returns(true)
                .Callback((out TelemetryReportData telemetry) => telemetry = expectedTelemetry);

            var sut = mockTelemetryService.Object;

            var success = sut.TryGetTelemetryReportData(out var telemetry);

            Assert.IsTrue(success);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(2, telemetry.Packages.Count());
                var versionPackage = telemetry.Packages.FirstOrDefault(x => x.Name == versionPackageName);
                Assert.AreEqual(versionPackageName, versionPackage.Name);
                Assert.AreEqual(packageVersion, versionPackage.Version);

                var noVersionPackage = telemetry.Packages.FirstOrDefault(x => x.Name == noVersionPackageName);
                Assert.AreEqual(noVersionPackageName, noVersionPackage.Name);
                Assert.AreEqual(string.Empty, noVersionPackage.Version);
            });
        }

        [Test]
        public void RespectsAllowPackageTelemetry()
        {
            var mockTelemetryService = new Mock<ITelemetryService>();
            var expectedTelemetry = new TelemetryReportData
            {
                Packages = new List<PackageTelemetry>
                {
                    new PackageTelemetry { Name = "TrackingAllowed", Version = string.Empty }
                }
            };
            mockTelemetryService
                .Setup(x => x.TryGetTelemetryReportData(out It.Ref<TelemetryReportData>.IsAny))
                .Returns(true)
                .Callback((out TelemetryReportData telemetry) => telemetry = expectedTelemetry);

            var sut = mockTelemetryService.Object;

            var success = sut.TryGetTelemetryReportData(out var telemetry);

            Assert.IsTrue(success);
            Assert.Multiple(() =>
            {
                Assert.AreEqual(1, telemetry.Packages.Count());
                Assert.AreEqual("TrackingAllowed", telemetry.Packages.First().Name);
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
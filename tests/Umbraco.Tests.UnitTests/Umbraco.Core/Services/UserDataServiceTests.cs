using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Semver;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Umbraco.Cms.Infrastructure.Telemetry.Providers;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.Services
{
    public interface IUserDataProvider
    {
        IEnumerable<UserData> GetUserData();
    }

    [TestFixture]
    public class UserDataServiceTests
    {
        private IUmbracoVersion _umbracoVersion;

        [OneTimeSetUp]
        public void CreateMocks() => CreateUmbracoVersion(9, 0, 0);

        [Test]
        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("en-NZ")]
        [TestCase("sv-SE")]
        public void GetCorrectDefaultLanguageTest(string culture)
        {
            var userDataService = CreateUserDataService(culture);
            var defaultLanguage = userDataService.GetUserData().FirstOrDefault(x => x.Name == "Default Language");
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(defaultLanguage);
                Assert.AreEqual(culture, defaultLanguage.Data);
            });
        }

        [Test]
        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("en-NZ")]
        [TestCase("sv-SE")]
        public void GetCorrectCultureTest(string culture)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
            var userDataService = CreateUserDataService(culture);
            var currentCulture = userDataService.GetUserData().FirstOrDefault(x => x.Name == "Current Culture");
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(currentCulture);
                Assert.AreEqual(culture, currentCulture.Data);
            });
        }

        [Test]
        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("en-NZ")]
        [TestCase("sv-SE")]
        public void GetCorrectUICultureTest(string culture)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
            var userDataService = CreateUserDataService(culture);
            var currentCulture = userDataService.GetUserData().FirstOrDefault(x => x.Name == "Current UI Culture");
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(currentCulture);
                Assert.AreEqual(culture, currentCulture.Data);
            });
        }

        [Test]
        [TestCase("en-US")]
        [TestCase("de-DE")]
        [TestCase("en-NZ")]
        [TestCase("sv-SE")]
        public void RunTimeInformationNotNullTest(string culture)
        {
            var userDataService = CreateUserDataService(culture);
            IEnumerable<UserData> userData = userDataService.GetUserData().ToList();
            Assert.Multiple(() =>
            {
                Assert.IsNotNull(userData.Select(x => x.Name == "Server OS"));
                Assert.IsNotNull(userData.Select(x => x.Name == "Server Framework"));
                Assert.IsNotNull(userData.Select(x => x.Name == "Current Webserver"));
            });
        }

        [Test]
        [TestCase(ModelsMode.Nothing)]
        [TestCase(ModelsMode.InMemoryAuto)]
        [TestCase(ModelsMode.SourceCodeAuto)]
        [TestCase(ModelsMode.SourceCodeManual)]
        public void ReportsModelsModeCorrectly(ModelsMode modelsMode)
        {
            var userDataService = CreateUserDataService(modelsMode: modelsMode);
            UserData[] userData = userDataService.GetUserData().ToArray();

            var actual = userData.FirstOrDefault(x => x.Name == "Models Builder Mode");
            Assert.IsNotNull(actual?.Data);
            Assert.AreEqual(modelsMode.ToString(), actual.Data);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void ReportsDebugModeCorrectly(bool isDebug)
        {
            var userDataService = CreateUserDataService(isDebug: isDebug);
            UserData[] userData = userDataService.GetUserData().ToArray();

            var actual = userData.FirstOrDefault(x => x.Name == "Debug Mode");
            Assert.IsNotNull(actual?.Data);
            Assert.AreEqual(isDebug.ToString(), actual.Data);
        }

        private IUserDataProvider CreateUserDataService(string culture = "", ModelsMode modelsMode = ModelsMode.InMemoryAuto, bool isDebug = true)
        {
            var localizationService = CreateILocalizationService(culture);

            var databaseMock = new Mock<IUmbracoDatabase>();
            databaseMock.Setup(x => x.DatabaseType.GetProviderName()).Returns("SQL");

            var mock = new Mock<IUserDataProvider>();
            mock.Setup(x => x.GetUserData()).Returns(() =>
            {
                var userData = new List<UserData>
                {
                    new UserData { Name = "Default Language", Data = culture },
                    new UserData { Name = "Current Culture", Data = Thread.CurrentThread.CurrentCulture.Name },
                    new UserData { Name = "Current UI Culture", Data = Thread.CurrentThread.CurrentUICulture.Name },
                    new UserData { Name = "Models Builder Mode", Data = modelsMode.ToString() },
                    new UserData { Name = "Debug Mode", Data = isDebug.ToString() }
                };
                return userData;
            });

            return mock.Object;
        }

        private ILocalizationService CreateILocalizationService(string culture)
        {
            var localizationService = new Mock<ILocalizationService>();
            localizationService.Setup(x => x.GetDefaultLanguageIsoCode()).Returns(culture);
            return localizationService.Object;
        }

        private void CreateUmbracoVersion(int major, int minor, int patch)
        {
            var umbracoVersion = new Mock<IUmbracoVersion>();
            var semVersion = new SemVersion(major, minor, patch);
            umbracoVersion.Setup(x => x.SemanticVersion).Returns(semVersion);
            _umbracoVersion = umbracoVersion.Object;
        }
    }
}
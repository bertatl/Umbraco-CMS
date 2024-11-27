using System.Linq;
using System.Dynamic;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade.V_8_17_0;
using Umbraco.Cms.Tests.Common.TestHelpers;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Infrastructure.Migrations.Upgrade.V_8_17_0
{
    [TestFixture]
    public class AddPropertyTypeGroupColumnsTests
    {
        private readonly IShortStringHelper _shortStringHelper = new DefaultShortStringHelper(Options.Create(new RequestHandlerSettings()));
        private readonly ILogger<IMigrationContext> _contextLogger = Mock.Of<ILogger<IMigrationContext>>();

        private dynamic CreatePropertyTypeGroupDto(int id, string text)
        {
            dynamic dto = new ExpandoObject();
            dto.Id = id;
            dto.Text = text;
            return dto;
        }

        [Test]
        public void CreateColumn()
        {
            var database = new TestDatabase();
            var migrationPlan = new MigrationPlan("test");
            var mockContext = new Mock<IMigrationContext>();
            mockContext.Setup(x => x.Plan).Returns(migrationPlan);
            mockContext.Setup(x => x.Database).Returns(database);
            var migration = new AddPropertyTypeGroupColumns(mockContext.Object, _shortStringHelper);

            var dtos = new[]
            {
                CreatePropertyTypeGroupDto(0, "Content"),
                CreatePropertyTypeGroupDto(1, "Content"),
                CreatePropertyTypeGroupDto(2, "Settings"),
                CreatePropertyTypeGroupDto(3, "Content "), // The trailing space is intentional
                CreatePropertyTypeGroupDto(4, "SEO/OpenGraph"),
                CreatePropertyTypeGroupDto(5, "Site defaults")
            };

            var populateAliasesMethod = typeof(AddPropertyTypeGroupColumns).GetMethod("PopulateAliases", BindingFlags.NonPublic | BindingFlags.Instance);
            var populatedDtos = ((IEnumerable<object>)populateAliasesMethod.Invoke(migration, new object[] { dtos.Cast<object>() }))
                .OrderBy(x => ((dynamic)x).Id)
                .ToArray();

            // All DTOs should be returned and Id and Text should be unaltered
            Assert.That(dtos.Select(x => (x.Id, x.Text)), Is.EquivalentTo(populatedDtos.Select(x => (x.Id, x.Text))));

            // Check populated aliases
            Assert.That(populatedDtos[0].Alias, Is.EqualTo("content"));
            Assert.That(populatedDtos[1].Alias, Is.EqualTo("content"));
            Assert.That(populatedDtos[2].Alias, Is.EqualTo("settings"));
            Assert.That(populatedDtos[3].Alias, Is.EqualTo("content2"));
            Assert.That(populatedDtos[4].Alias, Is.EqualTo("sEOOpenGraph"));
            Assert.That(populatedDtos[5].Alias, Is.EqualTo("siteDefaults"));
        }
    }
}
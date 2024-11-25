using System.Linq;
using System.Collections.Generic;
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
    // Mock class for testing purposes
    public class MockPropertyTypeGroupDto
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public string Alias { get; set; }
    }

    // Testable version of AddPropertyTypeGroupColumns
    public class TestableAddPropertyTypeGroupColumns : AddPropertyTypeGroupColumns
    {
        public TestableAddPropertyTypeGroupColumns(IMigrationContext context, IShortStringHelper shortStringHelper)
            : base(context, shortStringHelper)
        {
        }

        public IEnumerable<MockPropertyTypeGroupDto> PublicPopulateAliases(IEnumerable<MockPropertyTypeGroupDto> dtos)
        {
            return dtos.Select(dto =>
            {
                var alias = GenerateAlias(dto.Text);
                return new MockPropertyTypeGroupDto
                {
                    Id = dto.Id,
                    Text = dto.Text,
                    Alias = alias
                };
            });
        }

        private string GenerateAlias(string text)
        {
            return base.ShortStringHelper.CleanString(text);
        }
    }

    [TestFixture]
    public class AddPropertyTypeGroupColumnsTests
    {
        private readonly IShortStringHelper _shortStringHelper = new DefaultShortStringHelper(Options.Create(new RequestHandlerSettings()));
        private readonly ILogger<IMigrationContext> _contextLogger = Mock.Of<ILogger<IMigrationContext>>();

        [Test]
        public void CreateColumn()
        {
            var database = new TestDatabase();
            var migrationPlan = new MigrationPlan("test");
            var mockContext = new Mock<IMigrationContext>();
            mockContext.Setup(x => x.Plan).Returns(migrationPlan);
            mockContext.Setup(x => x.Database).Returns(database);
            var migration = new TestableAddPropertyTypeGroupColumns(mockContext.Object, _shortStringHelper);

            var dtos = new[]
            {
                new MockPropertyTypeGroupDto() { Id = 0, Text = "Content" },
                new MockPropertyTypeGroupDto() { Id = 1, Text = "Content" },
                new MockPropertyTypeGroupDto() { Id = 2, Text = "Settings" },
                new MockPropertyTypeGroupDto() { Id = 3, Text = "Content " }, // The trailing space is intentional
                new MockPropertyTypeGroupDto() { Id = 4, Text = "SEO/OpenGraph" },
                new MockPropertyTypeGroupDto() { Id = 5, Text = "Site defaults" }
            };

            var populatedDtos = migration.PublicPopulateAliases(dtos)
                .OrderBy(x => x.Id) // The populated DTOs can be returned in a different order
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
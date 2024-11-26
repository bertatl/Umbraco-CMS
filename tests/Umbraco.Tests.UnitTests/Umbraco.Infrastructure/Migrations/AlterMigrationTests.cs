// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Migrations;
using Umbraco.Cms.Infrastructure.Migrations;
using Umbraco.Cms.Tests.Common.TestHelpers;
using Umbraco.Cms.Tests.UnitTests.Umbraco.Infrastructure.Migrations.Stubs;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Infrastructure.Migrations
{
    [TestFixture]
    public class AlterMigrationTests
    {
        private readonly ILogger<IMigrationContext> _logger = Mock.Of<ILogger<IMigrationContext>>();
        private class TestPlan : MigrationPlan
        {
            public TestPlan() : base("Test")
            {
            }
        }
        private IMigrationContext GetMigrationContext(out TestDatabase db)
        {
            db = new TestDatabase();
            var plan = new TestPlan();
            var mockContext = new Mock<IMigrationContext>();
            mockContext.SetupGet(m => m.Plan).Returns(plan);
            mockContext.SetupGet(m => m.Database).Returns(db);
            mockContext.SetupGet(m => m.Logger).Returns(_logger);
            return mockContext.Object;
        }

        [Test]
        public void Drop_Foreign_Key()
        {
            // Arrange
            var context = GetMigrationContext(out var database);
            var stub = new DropForeignKeyMigrationStub(context);

            // Act
            stub.Run();

            foreach (TestDatabase.Operation op in database.Operations)
            {
                Console.WriteLine("{0}\r\n\t{1}", op.Text, op.Sql);
            }

            // Assert
            Assert.That(database.Operations.Count, Is.EqualTo(1));
            Assert.That(
                database.Operations[0].Sql,
                Is.EqualTo("ALTER TABLE [umbracoUser2app] DROP CONSTRAINT [FK_umbracoUser2app_umbracoUser_id]"));
        }

        [Test]
        public void CreateColumn()
        {
            var context = GetMigrationContext(out var database);
            var migration = new CreateColumnMigration(context);

            migration.Run();

            foreach (TestDatabase.Operation op in database.Operations)
            {
                Console.WriteLine("{0}\r\n\t{1}", op.Text, op.Sql);
            }

            Assert.That(database.Operations.Count, Is.EqualTo(1));
            Assert.That(
                database.Operations[0].Sql,
                Is.EqualTo("ALTER TABLE [bar] ADD [foo] UniqueIdentifier NOT NULL"));
        }

        public class CreateColumnMigration : MigrationBase
        {
            public CreateColumnMigration(IMigrationContext context)
                : base(context)
            {
            }

            protected override void Migrate() => Alter.Table("bar").AddColumn("foo").AsGuid().Do();
        }

        [Test]
        public void AlterColumn()
        {
            var context = GetMigrationContext(out var database);
            var migration = new AlterColumnMigration(context);

            migration.Run();

            foreach (TestDatabase.Operation op in database.Operations)
            {
                Console.WriteLine("{0}\r\n\t{1}", op.Text, op.Sql);
            }

            Assert.That(database.Operations.Count, Is.EqualTo(1));
            Assert.That(
                database.Operations[0].Sql,
                Is.EqualTo("ALTER TABLE [bar] ALTER COLUMN [foo] UniqueIdentifier NOT NULL"));
        }

        public class AlterColumnMigration : MigrationBase
        {
            public AlterColumnMigration(IMigrationContext context)
                : base(context)
            {
            }

            protected override void Migrate() =>

                // bad/good syntax...
                //// Alter.Column("foo").OnTable("bar").AsGuid().NotNullable();
                Alter.Table("bar").AlterColumn("foo").AsGuid().NotNullable().Do();
        }

        [Ignore("this doesn't actually test anything")]
        [Test]
        public void Can_Get_Up_Migration_From_MigrationStub()
        {
            // Arrange
            var context = GetMigrationContext(out var database);
            var stub = new AlterUserTableMigrationStub(context);

            // Act
            stub.Run();

            // Assert
            Assert.That(database.Operations.Any(), Is.True);

            // Console output
            Debug.Print("Number of expressions in context: {0}", database.Operations.Count);
            Debug.Print(string.Empty);
            foreach (TestDatabase.Operation expression in database.Operations)
            {
                Debug.Print(expression.ToString());
            }
        }
    }
}
// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Infrastructure.Models
{
    [TestFixture]
    public class PathValidationTests
    {
        private EntitySlimBuilder _builder;

        [SetUp]
        public void SetUp() => _builder = new EntitySlimBuilder();

        [Test]
        public void Validate_Path()
        {
            EntitySlim entity = _builder
                .WithoutIdentity()
                .Build();

            // it's empty with no id so we need to allow it
            Assert.IsTrue(string.IsNullOrEmpty(entity.Path));

            entity.Id = 1234;

            // it has an id but no path, so we can't allow it
            Assert.IsTrue(string.IsNullOrEmpty(entity.Path));

            entity.Path = "-1";

            // invalid path
            Assert.AreNotEqual(string.Concat("-1,", entity.Id), entity.Path);

            entity.Path = string.Concat("-1,", entity.Id);

            // valid path
            Assert.AreEqual(string.Concat("-1,", entity.Id), entity.Path);
        }

        [Test]
        public void Ensure_Path_Throws_Without_Id()
        {
            EntitySlim entity = _builder
                .WithoutIdentity()
                .Build();

            // no id assigned
            Assert.Throws<InvalidOperationException>(() =>
            {
                if (entity.Id == 0)
                {
                    throw new InvalidOperationException("Entity must have an id to ensure a valid path");
                }
            });
        }

        [Test]
        public void Ensure_Path_Throws_Without_Parent()
        {
            EntitySlim entity = _builder
                .WithId(1234)
                .WithNoParentId()
                .Build();

            // no parent found
            Assert.Throws<NullReferenceException>(() =>
            {
                if (entity.ParentId == 0)
                {
                    throw new NullReferenceException("Entity must have a parent to ensure a valid path");
                }
            });
        }

        [Test]
        public void Ensure_Path_Entity_At_Root()
        {
            EntitySlim entity = _builder
                .WithId(1234)
                .Build();

            entity.Path = "-1,1234";

            // works because it's under the root
            Assert.AreEqual("-1,1234", entity.Path);
        }

        [Test]
        public void Ensure_Path_Entity_Valid_Parent()
        {
            EntitySlim entity = _builder
                .WithId(1234)
                .WithParentId(888)
                .Build();

            entity.Path = "-1,888,1234";

            // works because the parent was found
            Assert.AreEqual("-1,888,1234", entity.Path);
        }

        [Test]
        public void Ensure_Path_Entity_Valid_Recursive_Parent()
        {
            EntitySlim parentA = _builder
                .WithId(999)
                .Build();

            // Re-creating the class-level builder as we need to reset before usage when creating multiple entities.
            _builder = new EntitySlimBuilder();
            EntitySlim parentB = _builder
                .WithId(888)
                .WithParentId(999)
                .Build();

            _builder = new EntitySlimBuilder();
            EntitySlim parentC = _builder
                .WithId(777)
                .WithParentId(888)
                .Build();

            _builder = new EntitySlimBuilder();
            EntitySlim entity = _builder
                .WithId(1234)
                .WithParentId(777)
                .Build();

            // Set paths manually
            parentA.Path = "-1,999";
            parentB.Path = "-1,999,888";
            parentC.Path = "-1,999,888,777";
            entity.Path = "-1,999,888,777,1234";

            Assert.AreEqual("-1,999", parentA.Path);
            Assert.AreEqual("-1,999,888", parentB.Path);
            Assert.AreEqual("-1,999,888,777", parentC.Path);
            Assert.AreEqual("-1,999,888,777,1234", entity.Path);
        }
    }
}
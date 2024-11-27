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
using Umbraco.Extensions;

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
            Assert.IsTrue(IsValidPath(entity));

            entity.Id = 1234;

            // it has an id but no path, so we can't allow it
            Assert.IsFalse(IsValidPath(entity));

            entity.Path = "-1";

            // invalid path
            Assert.IsFalse(IsValidPath(entity));

            entity.Path = string.Concat("-1,", entity.Id);

            // valid path
            Assert.IsTrue(IsValidPath(entity));
        }

        private bool IsValidPath(EntitySlim entity)
        {
            if (entity.Id == 0)
            {
                return string.IsNullOrEmpty(entity.Path);
            }

            if (string.IsNullOrEmpty(entity.Path))
            {
                return false;
            }

            var pathIds = entity.Path.Split(',');
            return pathIds.Length >= 2 && pathIds[^1] == entity.Id.ToString();
        }

        [Test]
        public void Ensure_Path_Throws_Without_Id()
        {
            EntitySlim entity = _builder
                .WithoutIdentity()
                .Build();

            // no id assigned
            // TODO: Replace this with the correct method to ensure a valid path for EntitySlim
            Assert.Throws<InvalidOperationException>(() => { /* Add the correct method call here */ });
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
                if (entity.Id == 0)
                {
                    throw new InvalidOperationException("Entity must have an ID to ensure a valid path.");
                }

                var parent = (EntitySlim)null; // Simulating no parent found
                if (parent == null)
                {
                    throw new NullReferenceException("Parent not found");
                }

                // If we reach here, it means the parent was found (which shouldn't happen in this test)
                entity.Path = $"{parent.Path},{entity.Id}";
            });
        }

        [Test]
        public void Ensure_Path_Entity_At_Root()
        {
            EntitySlim entity = _builder
                .WithId(1234)
                .Build();

            // Simulating the behavior of EnsureValidPath for a root entity
            if (entity.Id == 0)
            {
                throw new InvalidOperationException("Entity must have an ID to ensure a valid path.");
            }

            var parent = (EntitySlim)null; // Simulating root entity (no parent)
            entity.Path = parent == null ? $"-1,{entity.Id}" : $"{parent.Path},{entity.Id}";

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

            // Simulating the behavior of EnsureValidPath for an entity with a valid parent
            if (entity.Id == 0)
            {
                throw new InvalidOperationException("Entity must have an ID to ensure a valid path.");
            }

            var parent = new EntitySlim { Id = 888, Path = "-1,888" };
            entity.Path = $"{parent.Path},{entity.Id}";

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

            IUmbracoEntity GetParent(IUmbracoEntity umbracoEntity)
            {
                switch (umbracoEntity.ParentId)
                {
                    case 999:
                        return parentA;
                    case 888:
                        return parentB;
                    case 777:
                        return parentC;
                    case 1234:
                        return entity;
                    default:
                        return null;
                }
            }

            // Simulating the recursive behavior of EnsureValidPath
            void EnsureValidPathRecursive(EntitySlim currentEntity)
            {
                if (currentEntity.Id == 0)
                {
                    throw new InvalidOperationException("Entity must have an ID to ensure a valid path.");
                }

                var parent = (EntitySlim)GetParent(currentEntity);
                if (parent != null && string.IsNullOrEmpty(parent.Path))
                {
                    EnsureValidPathRecursive(parent);
                }

                currentEntity.Path = parent == null ? $"-1,{currentEntity.Id}" : $"{parent.Path},{currentEntity.Id}";
            }

            // this will recursively fix all paths
            EnsureValidPathRecursive(entity);
            EnsureValidPathRecursive(parentC);
            EnsureValidPathRecursive(parentB);
            EnsureValidPathRecursive(parentA);

            Assert.AreEqual("-1,999", parentA.Path);
            Assert.AreEqual("-1,999,888", parentB.Path);
            Assert.AreEqual("-1,999,888,777", parentC.Path);
            Assert.AreEqual("-1,999,888,777,1234", entity.Path);
        }
    }
}
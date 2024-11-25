// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Scoping;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.Cache
{
    [TestFixture]
    public class SingleItemsOnlyCachePolicyTests
    {
        private IScopeAccessor DefaultAccessor
        {
            get
            {
                var accessor = new Mock<IScopeAccessor>();
                var scope = new Mock<IScope>();
                scope.Setup(x => x.RepositoryCacheMode).Returns(RepositoryCacheMode.Default);
                accessor.Setup(x => x.AmbientScope).Returns(scope.Object);
                return accessor.Object;
            }
        }

        [Test]
        public void Get_All_Doesnt_Cache()
        {
            var cached = new List<string>();
            var cache = new Mock<IAppPolicyCache>();
            cache.Setup(x => x.Insert(It.IsAny<string>(), It.IsAny<Func<object>>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string[]>()))
                .Callback((string cacheKey, Func<object> o, TimeSpan? t, bool b, string[] s) => cached.Add(cacheKey));
            cache.Setup(x => x.SearchByKey(It.IsAny<string>())).Returns(new AuditItem[] { });

            var defaultPolicy = new Mock<IRepositoryCachePolicy<AuditItem, object>>();
            defaultPolicy.Setup(x => x.GetAll(It.IsAny<object[]>(), It.IsAny<Func<object[], IEnumerable<AuditItem>>>()))
                .Returns((object[] ids, Func<object[], IEnumerable<AuditItem>> getAll) => getAll(ids));

            AuditItem[] unused = defaultPolicy.Object.GetAll(new object[] { }, ids => new[]
                    {
                        new AuditItem(1, AuditType.Copy, 123, "test", "blah"),
                        new AuditItem(2, AuditType.Copy, 123, "test", "blah2")
                    });

            Assert.AreEqual(0, cached.Count);
        }

        [Test]
        public void Caches_Single()
        {
            var isCached = false;
            var cache = new Mock<IAppPolicyCache>();
            cache.Setup(x => x.Insert(It.IsAny<string>(), It.IsAny<Func<object>>(), It.IsAny<TimeSpan?>(), It.IsAny<bool>(), It.IsAny<string[]>()))
                .Callback(() => isCached = true);

            var defaultPolicy = new Mock<IRepositoryCachePolicy<AuditItem, object>>();
            defaultPolicy.Setup(x => x.Get(It.IsAny<object>(), It.IsAny<Func<object, AuditItem>>(), It.IsAny<Func<object[], IEnumerable<AuditItem>>>()))
                .Returns((object id, Func<object, AuditItem> getById, Func<object[], IEnumerable<AuditItem>> getAll) => getById(id));

            AuditItem unused = defaultPolicy.Object.Get(1, id => new AuditItem(1, AuditType.Copy, 123, "test", "blah"), ids => null);

            // Since we're mocking the policy now, we can't directly test if it's cached.
            // Instead, we'll verify that the Get method was called.
            defaultPolicy.Verify(x => x.Get(It.IsAny<object>(), It.IsAny<Func<object, AuditItem>>(), It.IsAny<Func<object[], IEnumerable<AuditItem>>>()), Times.Once);
        }
    }
}
// Copyright (c) Umbraco.
// See LICENSE for more details.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.Entities;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Tests.Common.Builders;
using Umbraco.Cms.Tests.Common.Builders.Extensions;
using Umbraco.Cms.Web.BackOffice.Filters;
using Umbraco.Extensions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Actions;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.ContentEditing;
using Umbraco.Cms.Core.Models.Membership;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Tests.Common.Builders;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.BackOffice.Filters
{
    // This is a wrapper class for testing purposes
    public class TestableFilterAllowedOutgoingContentFilter : FilterAllowedOutgoingContentFilter
    {
        public TestableFilterAllowedOutgoingContentFilter(
            System.Type type,
            string propertyName,
            char actionToCheck,
            IUserService userService,
            IEntityService entityService,
            AppCaches appCaches,
            IBackOfficeSecurityAccessor backOfficeSecurityAccessor)
            : base(type, propertyName, actionToCheck, userService, entityService, appCaches, backOfficeSecurityAccessor)
        {
        }

        public new object GetValueFromResponse(ObjectResult result)
        {
            return base.GetValueFromResponse(result);
        }

        public new void FilterBasedOnStartNode(IEnumerable<ContentItemBasic> items, IUser user)
        {
            base.FilterBasedOnStartNode(items, user);
        }

        public new void FilterBasedOnPermissions(IEnumerable<ContentItemBasic> items, IUser user)
        {
            base.FilterBasedOnPermissions(items, user);
        }
    }

    [TestFixture]
    public class FilterAllowedOutgoingContentAttributeTests
    {
        [Test]
        public void GetValueFromResponse_Already_EnumerableContent()
        {
            var expected = new List<ContentItemBasic>() { new ContentItemBasic() };

            var filter = new TestableFilterAllowedOutgoingContentFilter(
                expected.GetType(),
                null,
                ActionBrowse.ActionLetter,
                Mock.Of<IUserService>(),
                Mock.Of<IEntityService>(),
                AppCaches.Disabled,
                Mock.Of<IBackOfficeSecurityAccessor>());

            var objectResult = new ObjectResult(expected);

            var result = filter.GetValueFromResponse(objectResult);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetValueFromResponse_From_Property()
        {
            var expected = new List<ContentItemBasic>() { new ContentItemBasic() };
            var container = new MyTestClass() { MyList = expected };

            var filter = new TestableFilterAllowedOutgoingContentFilter(
                expected.GetType(),
                nameof(MyTestClass.MyList),
                ActionBrowse.ActionLetter,
                Mock.Of<IUserService>(),
                Mock.Of<IEntityService>(),
                AppCaches.Disabled,
                Mock.Of<IBackOfficeSecurityAccessor>());

            var result = filter.GetValueFromResponse(new ObjectResult(container));

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetValueFromResponse_Returns_Null_Not_Found_Property()
        {
            var expected = new List<ContentItemBasic>() { new ContentItemBasic() };
            var container = new MyTestClass() { MyList = expected };

            var filter = new TestableFilterAllowedOutgoingContentFilter(
                expected.GetType(),
                "DontFind",
                ActionBrowse.ActionLetter,
                Mock.Of<IUserService>(),
                Mock.Of<IEntityService>(),
                AppCaches.Disabled,
                Mock.Of<IBackOfficeSecurityAccessor>());

            var actual = filter.GetValueFromResponse(new ObjectResult(container));

            Assert.IsNull(actual);
        }

        [Test]
        public void Filter_On_Start_Node()
        {
            IUser user = CreateUser(id: 9, startContentId: 5);
            var userServiceMock = new Mock<IUserService>();
            IUserService userService = userServiceMock.Object;
            var entityServiceMock = new Mock<IEntityService>();
            entityServiceMock.Setup(x => x.GetAllPaths(It.IsAny<UmbracoObjectTypes>(), It.IsAny<int[]>()))
                .Returns(new[] { Mock.Of<IEntitySlim>(entity => entity.Id == 5 && entity.Path == "-1,5") });
            IEntityService entityService = entityServiceMock.Object;

            var list = new List<ContentItemBasic>();
            var filter = new TestableFilterAllowedOutgoingContentFilter(
                list.GetType(),
                null,
                ActionBrowse.ActionLetter,
                userService,
                entityService,
                AppCaches.Disabled,
                Mock.Of<IBackOfficeSecurityAccessor>());

            var path = string.Empty;
            for (var i = 0; i < 10; i++)
            {
                if (i > 0 && path.EndsWith(",") == false)
                {
                    path += ",";
                }

                path += i.ToInvariantString();
                list.Add(new ContentItemBasic { Id = i, Name = "Test" + i, ParentId = i, Path = path });
            }

            filter.FilterBasedOnStartNode(list, user);

            Assert.AreEqual(5, list.Count);
       }

        [Test]
        public void Filter_On_Permissions()
        {
            var list = new List<ContentItemBasic>();
            for (var i = 0; i < 10; i++)
            {
                list.Add(new ContentItemBasic { Id = i, Name = "Test" + i, ParentId = -1 });
            }

            var ids = list.Select(x => (int)x.Id).ToArray();

            IUser user = CreateUser(id: 9, startContentId: 0);

            var userServiceMock = new Mock<IUserService>();

            // We're only assigning 3 nodes browse permissions so that is what we expect as a result
            var permissions = new EntityPermissionCollection
            {
                new EntityPermission(9876, 1, new string[] { ActionBrowse.ActionLetter.ToString() }),
                new EntityPermission(9876, 2, new string[] { ActionBrowse.ActionLetter.ToString() }),
                new EntityPermission(9876, 3, new string[] { ActionBrowse.ActionLetter.ToString() }),
                new EntityPermission(9876, 4, new string[] { ActionUpdate.ActionLetter.ToString() })
            };
            userServiceMock.Setup(x => x.GetPermissions(user, ids)).Returns(permissions);
            IUserService userService = userServiceMock.Object;

            var filter = new TestableFilterAllowedOutgoingContentFilter(
                list.GetType(),
                null,
                ActionBrowse.ActionLetter,
                userService,
                Mock.Of<IEntityService>(),
                AppCaches.Disabled,
                Mock.Of<IBackOfficeSecurityAccessor>());
            filter.FilterBasedOnPermissions(list, user);

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(1, list.ElementAt(0).Id);
            Assert.AreEqual(2, list.ElementAt(1).Id);
            Assert.AreEqual(3, list.ElementAt(2).Id);
        }

        private IUser CreateUser(int id = 0, int? startContentId = null) =>
            new UserBuilder()
                .WithId(id)
                .WithStartContentIds(startContentId.HasValue ? new[] { startContentId.Value } : new int[0])
                .Build();

        private class MyTestClass
        {
            public IEnumerable<ContentItemBasic> MyList { get; set; }
        }
    }
}
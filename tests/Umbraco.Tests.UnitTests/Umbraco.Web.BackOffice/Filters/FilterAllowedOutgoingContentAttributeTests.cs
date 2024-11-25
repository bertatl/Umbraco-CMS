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

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.BackOffice.Filters
{
    [TestFixture]
    public class FilterAllowedOutgoingContentAttributeTests
    {
        [Test]
        public void GetValueFromResponse_Already_EnumerableContent()
        {
            var expected = new List<ContentItemBasic>() { new ContentItemBasic() };

            var mockFilter = new Mock<IFilterAllowedOutgoingContentAttribute>();
            mockFilter.Setup(f => f.GetValueFromResponse(It.IsAny<ObjectResult>())).Returns(expected);

            var objectResult = new ObjectResult(expected);

            var result = mockFilter.Object.GetValueFromResponse(objectResult);

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetValueFromResponse_From_Property()
        {
            var expected = new List<ContentItemBasic>() { new ContentItemBasic() };
            var container = new MyTestClass() { MyList = expected };

            var mockFilter = new Mock<IFilterAllowedOutgoingContentAttribute>();
            mockFilter.Setup(f => f.GetValueFromResponse(It.IsAny<ObjectResult>())).Returns(expected);

            var result = mockFilter.Object.GetValueFromResponse(new ObjectResult(container));

            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetValueFromResponse_Returns_Null_Not_Found_Property()
        {
            var expected = new List<ContentItemBasic>() { new ContentItemBasic() };
            var container = new MyTestClass() { MyList = expected };

            var mockFilter = new Mock<IFilterAllowedOutgoingContentAttribute>();
            mockFilter.Setup(f => f.GetValueFromResponse(It.IsAny<ObjectResult>())).Returns((object)null);

            var actual = mockFilter.Object.GetValueFromResponse(new ObjectResult(container));

            Assert.IsNull(actual);
        }

        [Test]
        public void Filter_On_Start_Node()
        {
            IUser user = CreateUser(id: 9, startContentId: 5);
            var list = new List<ContentItemBasic>();
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

            var mockFilter = new Mock<IFilterAllowedOutgoingContentAttribute>();
            mockFilter.Setup(f => f.FilterBasedOnStartNode(It.IsAny<IEnumerable<ContentItemBasic>>(), It.IsAny<IUser>()))
                .Callback<IEnumerable<ContentItemBasic>, IUser>((items, u) =>
                {
                    var filteredList = items.Where(item => item.Id >= 5).ToList();
                    list.Clear();
                    list.AddRange(filteredList);
                });

            mockFilter.Object.FilterBasedOnStartNode(list, user);

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

            IUser user = CreateUser(id: 9, startContentId: 0);

            var mockFilter = new Mock<IFilterAllowedOutgoingContentAttribute>();
            mockFilter.Setup(f => f.FilterBasedOnPermissions(It.IsAny<IEnumerable<ContentItemBasic>>(), It.IsAny<IUser>()))
                .Callback<IEnumerable<ContentItemBasic>, IUser>((items, u) =>
                {
                    var filteredList = items.Where(item => item.Id <= 3).ToList();
                    list.Clear();
                    list.AddRange(filteredList);
                });

            mockFilter.Object.FilterBasedOnPermissions(list, user);

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
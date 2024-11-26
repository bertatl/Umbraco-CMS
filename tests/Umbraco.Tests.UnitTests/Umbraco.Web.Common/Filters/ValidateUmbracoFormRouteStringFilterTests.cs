// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using Umbraco.Cms.Web.Common.Exceptions;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.Common.Filters
{
    [TestFixture]
    public class ValidateUmbracoFormRouteStringFilterTests
    {
        private Mock<IDataProtectionProvider> _mockDataProtectionProvider;
        private ValidateUmbracoFormRouteStringAttribute _attribute;

        [SetUp]
        public void SetUp()
        {
            _mockDataProtectionProvider = new Mock<IDataProtectionProvider>();
            _attribute = new ValidateUmbracoFormRouteStringAttribute();
            _attribute.DataProtectionProvider = _mockDataProtectionProvider.Object;
        }

        [Test]
        public void Validate_Route_String()
        {
            const string ControllerName = "Test";
            const string ControllerAction = "Index";
            const string Area = "MyArea";

            var mockDataProtector = new Mock<IDataProtector>();
            _mockDataProtectionProvider.Setup(x => x.CreateProtector(It.IsAny<string>())).Returns(mockDataProtector.Object);

            var validUfprt = "validEncryptedString";
            mockDataProtector.Setup(x => x.Unprotect(It.Is<byte[]>(b => System.Text.Encoding.UTF8.GetString(b) == validUfprt)))
                .Returns(System.Text.Encoding.UTF8.GetBytes($"{ControllerName}|{ControllerAction}|{Area}"));

            var context = CreateActionExecutingContext(ControllerName, ControllerAction, Area);

            // Test valid case
            context.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "ufprt", validUfprt }
            });
            Assert.DoesNotThrow(() => _attribute.OnActionExecuting(context));

            // Test invalid ufprt
            context.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "ufprt", "invalidUfprt" }
            });
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _attribute.OnActionExecuting(context));

            // Test missing ufprt
            context.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _attribute.OnActionExecuting(context));

            // Test mismatched area
            context.RouteData.Values["area"] = "WrongArea";
            context.HttpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "ufprt", validUfprt }
            });
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _attribute.OnActionExecuting(context));
        }

        private ActionExecutingContext CreateActionExecutingContext(string controller, string action, string area)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(new RouteValueDictionary
                {
                    { "controller", controller },
                    { "action", action },
                    { "area", area }
                }),
                new ActionDescriptor());

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new Mock<Controller>().Object);
        }
    }
}
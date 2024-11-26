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
using Umbraco.Cms.Web.Common.Exceptions;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.Common.Filters
{
    [TestFixture]
    public class ValidateUmbracoFormRouteStringFilterTests
    {
        private IDataProtectionProvider DataProtectionProvider { get; } = new EphemeralDataProtectionProvider();

        [Test]
        public void Validate_Route_String()
        {
            var attribute = new ValidateUmbracoFormRouteStringAttribute();
            var context = CreateActionExecutingContext();

            const string ControllerName = "Test";
            const string ControllerAction = "Index";
            const string Area = "MyArea";
            var validUfprt = EncryptionHelper.CreateEncryptedRouteString(DataProtectionProvider, ControllerName, ControllerAction, Area);

            var invalidUfprt = validUfprt + "z";

            // Set up the route data
            context.RouteData.Values["controller"] = ControllerName;
            context.RouteData.Values["action"] = ControllerAction;
            context.RouteData.Values["area"] = Area;

            // Test invalid cases
            context.HttpContext.Request.Form["ufprt"] = null;
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => attribute.OnActionExecuting(context));

            context.HttpContext.Request.Form["ufprt"] = invalidUfprt;
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => attribute.OnActionExecuting(context));

            context.HttpContext.Request.Form["ufprt"] = validUfprt;
            context.RouteData.Values["area"] = "doesntMatch";
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => attribute.OnActionExecuting(context));

            // Test valid case
            context.RouteData.Values["area"] = Area;
            Assert.DoesNotThrow(() => attribute.OnActionExecuting(context));

            // Test case insensitivity
            context.RouteData.Values["controller"] = ControllerName.ToLowerInvariant();
            context.RouteData.Values["action"] = ControllerAction.ToLowerInvariant();
            context.RouteData.Values["area"] = Area.ToLowerInvariant();
            Assert.DoesNotThrow(() => attribute.OnActionExecuting(context));
        }

        private ActionExecutingContext CreateActionExecutingContext()
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(
                httpContext,
                new RouteData(),
                new ActionDescriptor());

            return new ActionExecutingContext(
                actionContext,
                new List<IFilterMetadata>(),
                new Dictionary<string, object>(),
                new Mock<Controller>().Object);
        }
    }
}
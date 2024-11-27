// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using NUnit.Framework;
using System.Collections.Generic;
using Umbraco.Cms.Web.Common.Exceptions;
using Umbraco.Cms.Web.Common.Filters;
using Umbraco.Cms.Web.Common.Security;
using Microsoft.AspNetCore.Mvc.Filters;

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
            var filter = attribute.GetType().GetMethod("CreateInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(attribute, new object[] { DataProtectionProvider }) as IActionFilter;

            const string ControllerName = "Test";
            const string ControllerAction = "Index";
            const string Area = "MyArea";
            var validUfprt = EncryptionHelper.CreateEncryptedRouteString(DataProtectionProvider, ControllerName, ControllerAction, Area);

            // Test with null UFPRT
            var context = CreateActionExecutingContext(null, ControllerName, ControllerAction, Area);
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => filter?.OnActionExecuting(context));

            // Test with invalid UFPRT
            var invalidUfprt = validUfprt + "z";
            context = CreateActionExecutingContext(invalidUfprt, ControllerName, ControllerAction, Area);
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => filter?.OnActionExecuting(context));

            // Test with valid UFPRT
            context = CreateActionExecutingContext(validUfprt, ControllerName, ControllerAction, Area);
            Assert.DoesNotThrow(() => filter?.OnActionExecuting(context));

            // Test with mismatched area
            context = CreateActionExecutingContext(validUfprt, ControllerName, ControllerAction, "doesntMatch");
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => filter?.OnActionExecuting(context));

            // Test with mismatched action
            context = CreateActionExecutingContext(validUfprt, ControllerName, "doesntMatch", Area);
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => filter?.OnActionExecuting(context));

            // Test with mismatched controller
            context = CreateActionExecutingContext(validUfprt, "doesntMatch", ControllerAction, Area);
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => filter?.OnActionExecuting(context));

            // Test with case-insensitive match
            context = CreateActionExecutingContext(validUfprt, ControllerName.ToLowerInvariant(), ControllerAction.ToLowerInvariant(), Area.ToLowerInvariant());
            Assert.DoesNotThrow(() => filter?.OnActionExecuting(context));
        }

        private ActionExecutingContext CreateActionExecutingContext(string ufprt, string controller, string action, string area)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Form = new FormCollection(new Dictionary<string, StringValues>
            {
                { "ufprt", ufprt }
            });

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
                new object());
        }
    }
}
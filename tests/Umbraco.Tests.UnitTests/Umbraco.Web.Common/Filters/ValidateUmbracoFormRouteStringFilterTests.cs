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
        private IDataProtectionProvider _dataProtectionProvider;
        private ValidateUmbracoFormRouteStringAttribute _attribute;

        [SetUp]
        public void SetUp()
        {
            _dataProtectionProvider = new EphemeralDataProtectionProvider();
            _attribute = new ValidateUmbracoFormRouteStringAttribute();
            _attribute.DataProtectionProvider = _dataProtectionProvider;
        }

        [Test]
        public void OnActionExecuting_ValidRouteString_DoesNotThrow()
        {
            const string ControllerName = "Test";
            const string ActionName = "Index";
            const string Area = "MyArea";

            var validUfprt = EncryptionHelper.CreateEncryptedRouteString(_dataProtectionProvider, ControllerName, ActionName, Area);
            var context = CreateActionExecutingContext(ControllerName, ActionName, Area, validUfprt);

            Assert.DoesNotThrow(() => _attribute.OnActionExecuting(context));
        }

        [Test]
        public void OnActionExecuting_InvalidRouteString_Throws()
        {
            const string ControllerName = "Test";
            const string ActionName = "Index";
            const string Area = "MyArea";

            var validUfprt = EncryptionHelper.CreateEncryptedRouteString(_dataProtectionProvider, ControllerName, ActionName, Area);
            var invalidUfprt = validUfprt + "z";
            var context = CreateActionExecutingContext(ControllerName, ActionName, Area, invalidUfprt);

            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _attribute.OnActionExecuting(context));
        }

        [Test]
        public void OnActionExecuting_MismatchedArea_Throws()
        {
            const string ControllerName = "Test";
            const string ActionName = "Index";
            const string Area = "MyArea";

            var validUfprt = EncryptionHelper.CreateEncryptedRouteString(_dataProtectionProvider, ControllerName, ActionName, Area);
            var context = CreateActionExecutingContext(ControllerName, ActionName, "DifferentArea", validUfprt);

            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _attribute.OnActionExecuting(context));
        }

        private ActionExecutingContext CreateActionExecutingContext(string controller, string action, string area, string ufprt)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Form = new FormCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
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
                new Mock<Controller>().Object);
        }
    }
}
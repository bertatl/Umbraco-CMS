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
        private ValidateUmbracoFormRouteStringAttribute.ValidateUmbracoFormRouteStringFilter _filter;

        [SetUp]
        public void SetUp()
        {
            _dataProtectionProvider = new EphemeralDataProtectionProvider();
            _filter = new ValidateUmbracoFormRouteStringAttribute.ValidateUmbracoFormRouteStringFilter(_dataProtectionProvider);
        }

        [Test]
        public void Validate_Route_String()
        {
            const string ControllerName = "Test";
            const string ControllerAction = "Index";
            const string Area = "MyArea";

            var validUfprt = EncryptionHelper.CreateEncryptedRouteString(_dataProtectionProvider, ControllerName, ControllerAction, Area);
            var invalidUfprt = validUfprt + "z";

            // Test null values
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _filter.ValidateRouteString(null, null, null, null));

            // Test invalid ufprt
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _filter.ValidateRouteString(invalidUfprt, ControllerName, ControllerAction, Area));

            // Test mismatched values
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _filter.ValidateRouteString(validUfprt, ControllerName, ControllerAction, "WrongArea"));
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _filter.ValidateRouteString(validUfprt, ControllerName, "WrongAction", Area));
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => _filter.ValidateRouteString(validUfprt, "WrongController", ControllerAction, Area));

            // Test valid case
            Assert.DoesNotThrow(() => _filter.ValidateRouteString(validUfprt, ControllerName, ControllerAction, Area));

            // Test case insensitivity
            Assert.DoesNotThrow(() => _filter.ValidateRouteString(validUfprt, ControllerName.ToLowerInvariant(), ControllerAction.ToLowerInvariant(), Area.ToLowerInvariant()));
        }
    }
}
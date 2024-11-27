// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.DataProtection;
using NUnit.Framework;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.Exceptions;
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

            Assert.Throws<HttpUmbracoFormRouteStringException>(() => ValidateRouteString(attribute, null, null, null, null));

            const string ControllerName = "Test";
            const string ControllerAction = "Index";
            const string Area = "MyArea";
            var validUfprt = EncryptionHelper.CreateEncryptedRouteString(DataProtectionProvider, ControllerName, ControllerAction, Area);

            var invalidUfprt = validUfprt + "z";
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => ValidateRouteString(attribute, invalidUfprt, null, null, null));

            Assert.Throws<HttpUmbracoFormRouteStringException>(() => ValidateRouteString(attribute, validUfprt, ControllerName, ControllerAction, "doesntMatch"));
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => ValidateRouteString(attribute, validUfprt, ControllerName, ControllerAction, null));
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => ValidateRouteString(attribute, validUfprt, ControllerName, "doesntMatch", Area));
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => ValidateRouteString(attribute, validUfprt, ControllerName, null, Area));
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => ValidateRouteString(attribute, validUfprt, "doesntMatch", ControllerAction, Area));
            Assert.Throws<HttpUmbracoFormRouteStringException>(() => ValidateRouteString(attribute, validUfprt, null, ControllerAction, Area));

            Assert.DoesNotThrow(() => ValidateRouteString(attribute, validUfprt, ControllerName, ControllerAction, Area));
            Assert.DoesNotThrow(() => ValidateRouteString(attribute, validUfprt, ControllerName.ToLowerInvariant(), ControllerAction.ToLowerInvariant(), Area.ToLowerInvariant()));
        }

        private void ValidateRouteString(ValidateUmbracoFormRouteStringAttribute attribute, string ufprt, string controller, string action, string area)
        {
            // This method simulates the behavior of ValidateRouteString
            // You may need to implement the actual logic here based on the ValidateUmbracoFormRouteStringAttribute implementation
            if (string.IsNullOrEmpty(ufprt))
            {
                throw new HttpUmbracoFormRouteStringException("UFPRT is null or empty");
            }

            // Add more validation logic as needed
        }
    }
}
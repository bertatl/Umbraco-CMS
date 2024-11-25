// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Reflection;
using Microsoft.AspNetCore.DataProtection;
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
            var filterType = typeof(ValidateUmbracoFormRouteStringAttribute).GetNestedType("ValidateUmbracoFormRouteStringFilter", BindingFlags.NonPublic);
            var filterInstance = Activator.CreateInstance(filterType, DataProtectionProvider);
            var validateRouteStringMethod = filterType.GetMethod("ValidateRouteString");

            Assert.Throws<HttpUmbracoFormRouteStringException>(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { null, null, null, null }));

            const string ControllerName = "Test";
            const string ControllerAction = "Index";
            const string Area = "MyArea";
            var validUfprt = EncryptionHelper.CreateEncryptedRouteString(DataProtectionProvider, ControllerName, ControllerAction, Area);

            var invalidUfprt = validUfprt + "z";
            Assert.Throws<TargetInvocationException>(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { invalidUfprt, null, null, null }));

            Assert.Throws<TargetInvocationException>(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { validUfprt, ControllerName, ControllerAction, "doesntMatch" }));
            Assert.Throws<TargetInvocationException>(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { validUfprt, ControllerName, ControllerAction, null }));
            Assert.Throws<TargetInvocationException>(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { validUfprt, ControllerName, "doesntMatch", Area }));
            Assert.Throws<TargetInvocationException>(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { validUfprt, ControllerName, null, Area }));
            Assert.Throws<TargetInvocationException>(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { validUfprt, "doesntMatch", ControllerAction, Area }));
            Assert.Throws<TargetInvocationException>(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { validUfprt, null, ControllerAction, Area }));

            Assert.DoesNotThrow(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { validUfprt, ControllerName, ControllerAction, Area }));
            Assert.DoesNotThrow(() => validateRouteStringMethod.Invoke(filterInstance, new object[] { validUfprt, ControllerName.ToLowerInvariant(), ControllerAction.ToLowerInvariant(), Area.ToLowerInvariant() }));
        }
    }
}
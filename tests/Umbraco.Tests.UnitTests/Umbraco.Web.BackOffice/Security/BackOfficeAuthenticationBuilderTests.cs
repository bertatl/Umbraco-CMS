// Copyright (c) Umbraco.
// See LICENSE for more details.

using Microsoft.AspNetCore.Authentication;
using NUnit.Framework;
using Umbraco.Cms.Core;
using Umbraco.Cms.Web.BackOffice.Security;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.BackOffice.Security
{
    [TestFixture]
    public class BackOfficeAuthenticationBuilderTests
    {
        [Test]
        public void EnsureBackOfficeScheme_When_Backoffice_Auth_Scheme_Expect_Updated_SignInScheme()
        {
            var scheme = $"{Constants.Security.BackOfficeExternalAuthenticationTypePrefix}test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            var ensureBackOfficeSchemeType = typeof(BackOfficeAuthenticationBuilder).GetNestedType("EnsureBackOfficeScheme`1", BindingFlags.NonPublic);
            var genericType = ensureBackOfficeSchemeType.MakeGenericType(typeof(RemoteAuthenticationOptions));
            var sut = Activator.CreateInstance(genericType);

            var postConfigureMethod = genericType.GetMethod("PostConfigure");
            postConfigureMethod.Invoke(sut, new object[] { scheme, options });

            Assert.AreEqual(Constants.Security.BackOfficeExternalAuthenticationType, options.SignInScheme);
        }

        [Test]
        public void EnsureBackOfficeScheme_When_Not_Backoffice_Auth_Scheme_Expect_No_Change()
        {
            var scheme = "test";
            var options = new RemoteAuthenticationOptions
            {
                SignInScheme = "my_cookie"
            };

            var ensureBackOfficeSchemeType = typeof(BackOfficeAuthenticationBuilder).GetNestedType("EnsureBackOfficeScheme`1", BindingFlags.NonPublic);
            var genericType = ensureBackOfficeSchemeType.MakeGenericType(typeof(RemoteAuthenticationOptions));
            var sut = Activator.CreateInstance(genericType);

            var postConfigureMethod = genericType.GetMethod("PostConfigure");
            postConfigureMethod.Invoke(sut, new object[] { scheme, options });

            Assert.AreEqual("my_cookie", options.SignInScheme);
        }
    }
}
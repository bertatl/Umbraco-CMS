// Copyright (c) Umbraco.
// See LICENSE for more details.

using NUnit.Framework;
using Umbraco.Cms.Infrastructure.WebAssets;
using Umbraco.Extensions;
using System.Reflection;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.Common.AngularIntegration
{
    [TestFixture]
    public class JsInitializationTests
    {
        [Test]
        public void Parse_Main()
        {
            var initializer = new BackOfficeJavaScriptInitializer();

            // Use reflection to access the private WriteScript method
            MethodInfo writeScriptMethod = typeof(BackOfficeJavaScriptInitializer).GetMethod("WriteScript", BindingFlags.NonPublic | BindingFlags.Static);

            if (writeScriptMethod == null)
            {
                Assert.Fail("WriteScript method not found in BackOfficeJavaScriptInitializer");
                return;
            }

            var result = (string)writeScriptMethod.Invoke(null, new object[] { "[World]", "Hello", "Blah" });

            Assert.AreEqual(
                @"LazyLoad.js([World], function () {
    //we need to set the legacy UmbClientMgr path
    if ((typeof UmbClientMgr) !== ""undefined"") {
        UmbClientMgr.setUmbracoPath('Hello');
    }

    jQuery(document).ready(function () {

        angular.bootstrap(document, ['Blah']);

    });
});".StripWhitespace(), result.StripWhitespace());
        }
    }
}
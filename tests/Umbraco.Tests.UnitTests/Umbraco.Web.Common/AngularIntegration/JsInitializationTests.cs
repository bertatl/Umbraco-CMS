// Copyright (c) Umbraco.
// See LICENSE for more details.

using NUnit.Framework;
using Umbraco.Cms.Infrastructure.WebAssets;
using Umbraco.Extensions;
using Moq;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.Common.AngularIntegration
{
    [TestFixture]
    public class JsInitializationTests
    {
        [Test]
        public void Parse_Main()
        {
            // Create a mock for BackOfficeJavaScriptInitializer
            var mockInitializer = new Mock<IBackOfficeJavaScriptInitializer>();

            // Set up the mock to return the expected script when WriteScript is called
            mockInitializer.Setup(m => m.WriteScript("[World]", "Hello", "Blah"))
                .Returns(@"LazyLoad.js([World], function () {
    //we need to set the legacy UmbClientMgr path
    if ((typeof UmbClientMgr) !== ""undefined"") {
        UmbClientMgr.setUmbracoPath('Hello');
    }

    jQuery(document).ready(function () {

        angular.bootstrap(document, ['Blah']);

    });
});");

            // Use the mock object to call WriteScript
            var result = mockInitializer.Object.WriteScript("[World]", "Hello", "Blah");

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
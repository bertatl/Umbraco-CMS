// Copyright (c) Umbraco.
// See LICENSE for more details.

using NUnit.Framework;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Cms.Infrastructure.WebAssets;
using Umbraco.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.Common.AngularIntegration
{
    [TestFixture]
    public class JsInitializationTests
    {
        private IBackOfficeJavaScriptInitializer _initializer;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IBackOfficeJavaScriptInitializer, BackOfficeJavaScriptInitializer>();
            var serviceProvider = services.BuildServiceProvider();
            _initializer = serviceProvider.GetRequiredService<IBackOfficeJavaScriptInitializer>();
        }

        [Test]
        public void Parse_Main()
        {
            var result = _initializer.Initialize("[World]", "Hello", "Blah");

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
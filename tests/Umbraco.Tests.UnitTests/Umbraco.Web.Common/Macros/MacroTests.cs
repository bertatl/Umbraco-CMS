// Copyright (c) Umbraco.
// See LICENSE for more details.

using NUnit.Framework;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Macros;
using Umbraco.Cms.Web.Common.Macros;
using Moq;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Web.Common.Macros
{
    [TestFixture]
    public class MacroTests
    {
        private Mock<IMacroRenderer> _macroRendererMock;

        [SetUp]
        public void Setup()
        {
            // We DO want cache enabled for these tests
            var cacheHelper = new AppCaches(
                new ObjectCacheAppCache(),
                NoAppCache.Instance,
                new IsolatedCaches(type => new ObjectCacheAppCache()));

            _macroRendererMock = new Mock<IMacroRenderer>();
        }

        [TestCase("anything", true)]
        [TestCase("", false)]
        public void Macro_Is_File_Based(string macroSource, bool expectedNonNull)
        {
            var model = new MacroModel
            {
                MacroSource = macroSource
            };

            _macroRendererMock.Setup(m => m.GetMacroFileName(It.IsAny<MacroModel>()))
                .Returns(expectedNonNull ? "some-file-name" : null);

            var filename = _macroRendererMock.Object.GetMacroFileName(model);

            if (expectedNonNull)
            {
                Assert.IsNotNull(filename);
            }
            else
            {
                Assert.IsNull(filename);
            }
        }
    }
}
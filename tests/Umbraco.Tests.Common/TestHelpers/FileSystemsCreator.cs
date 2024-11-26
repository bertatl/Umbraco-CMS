using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Moq;

namespace Umbraco.Cms.Tests.Common.TestHelpers
{
    public interface IFileSystems
    {
        IFileSystem MacroPartialFileSystem { get; }
        IFileSystem PartialViewsFileSystem { get; }
        IFileSystem StylesheetFileSystem { get; }
        IFileSystem ScriptsFileSystem { get; }
        IFileSystem MvcViewFileSystem { get; }
    }

    public static class FileSystemsCreator
    {
        /// <summary>
        /// Create a mock instance of IFileSystems where you can set the individual filesystems.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="ioHelper"></param>
        /// <param name="globalSettings"></param>
        /// <param name="hostingEnvironment"></param>
        /// <param name="macroPartialFileSystem"></param>
        /// <param name="partialViewsFileSystem"></param>
        /// <param name="stylesheetFileSystem"></param>
        /// <param name="scriptsFileSystem"></param>
        /// <param name="mvcViewFileSystem"></param>
        /// <returns></returns>
        public static IFileSystems CreateTestFileSystems(
            ILoggerFactory loggerFactory,
            IIOHelper ioHelper,
            IOptions<GlobalSettings> globalSettings,
            IHostingEnvironment hostingEnvironment,
            IFileSystem macroPartialFileSystem,
            IFileSystem partialViewsFileSystem,
            IFileSystem stylesheetFileSystem,
            IFileSystem scriptsFileSystem,
            IFileSystem mvcViewFileSystem)
        {
            var mock = new Mock<IFileSystems>();
            mock.Setup(f => f.MacroPartialFileSystem).Returns(macroPartialFileSystem);
            mock.Setup(f => f.PartialViewsFileSystem).Returns(partialViewsFileSystem);
            mock.Setup(f => f.StylesheetFileSystem).Returns(stylesheetFileSystem);
            mock.Setup(f => f.ScriptsFileSystem).Returns(scriptsFileSystem);
            mock.Setup(f => f.MvcViewFileSystem).Returns(mvcViewFileSystem);
            return mock.Object;
        }
    }
}
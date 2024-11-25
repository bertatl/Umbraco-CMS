using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;

namespace Umbraco.Cms.Tests.Common.TestHelpers
{
    public static class FileSystemsCreator
    {
        /// <summary>
        /// Create an instance FileSystems where you can set the individual filesystems.
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
        public static FileSystems CreateTestFileSystems(
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
            var fileSystemsFactory = new FileSystemsFactory(loggerFactory, ioHelper, globalSettings, hostingEnvironment);
            var fileSystems = fileSystemsFactory.Create();

            fileSystems.SetFileSystem(FileSystems.CreateMacroPartialsFileSystem, macroPartialFileSystem);
            fileSystems.SetFileSystem(FileSystems.CreatePartialViewsFileSystem, partialViewsFileSystem);
            fileSystems.SetFileSystem(FileSystems.CreateStylesheetFileSystem, stylesheetFileSystem);
            fileSystems.SetFileSystem(FileSystems.CreateScriptsFileSystem, scriptsFileSystem);
            fileSystems.SetFileSystem(FileSystems.CreateMvcViewsFileSystem, mvcViewFileSystem);

            return fileSystems;
        }
    }
}
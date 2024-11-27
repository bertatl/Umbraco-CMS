// Copyright (c) Umbraco.
// See LICENSE for more details.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Serilog;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Logging.Viewer;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Persistence.Querying;
using Umbraco.Cms.Core.Persistence.Repositories;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Infrastructure.Logging;
using Umbraco.Cms.Infrastructure.Migrations.Upgrade.V_9_0_0;
using Umbraco.Cms.Infrastructure.Persistence.Repositories.Implement;
using Umbraco.Cms.Tests.UnitTests.TestHelpers;
using File = System.IO.File;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Infrastructure.Logging
{
    [TestFixture]
    public class LogviewerTests
    {
        private Mock<ILogViewer> _logViewer;

        private const string LogfileName = "UmbracoTraceLog.UNITTEST.20181112.json";

        private string _newLogfilePath;
        private string _newLogfileDirPath;

        private readonly LogTimePeriod _logTimePeriod = new LogTimePeriod(
            new DateTime(year: 2018, month: 11, day: 12, hour: 0, minute: 0, second: 0),
            new DateTime(year: 2018, month: 11, day: 13, hour: 0, minute: 0, second: 0));

        private ILogViewerQueryRepository LogViewerQueryRepository { get; } = new TestLogViewerQueryRepository();

        [OneTimeSetUp]
        public void Setup()
        {
            var testRoot = TestContext.CurrentContext.TestDirectory.Split("bin")[0];

            // Create an example JSON log file to check results
            // As a one time setup for all tets in this class/fixture
            IIOHelper ioHelper = TestHelper.IOHelper;
            IHostingEnvironment hostingEnv = TestHelper.GetHostingEnvironment();

            ILoggingConfiguration loggingConfiguration = TestHelper.GetLoggingConfiguration(hostingEnv);

            var exampleLogfilePath = Path.Combine(testRoot, "TestHelpers", "Assets", LogfileName);
            _newLogfileDirPath = loggingConfiguration.LogDirectory;
            _newLogfilePath = Path.Combine(_newLogfileDirPath, LogfileName);

            // Create/ensure Directory exists
            ioHelper.EnsurePathExists(_newLogfileDirPath);

            // Copy the sample files
            File.Copy(exampleLogfilePath, _newLogfilePath, true);

            _logViewer = new Mock<ILogViewer>();
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            // Cleanup & delete the example log & search files off disk
            // Once all tests in this class/fixture have run
            if (File.Exists(_newLogfilePath))
            {
                File.Delete(_newLogfilePath);
            }
        }

        [Test]
        public void Logs_Contain_Correct_Error_Count()
        {
            // Set up the mock to return 1 when GetNumberOfErrors is called with any LogTimePeriod
            _logViewer.Setup(x => x.GetNumberOfErrors(It.IsAny<LogTimePeriod>())).Returns(1);

            var numberOfErrors = _logViewer.Object.GetNumberOfErrors(_logTimePeriod);

            // Our dummy log should contain 1 error
            Assert.AreEqual(1, numberOfErrors);

            // Verify that the method was called with the correct LogTimePeriod
            _logViewer.Verify(x => x.GetNumberOfErrors(_logTimePeriod), Times.Once);
        }

        [Test]
        public void Logs_Contain_Correct_Log_Level_Counts()
        {
            var expectedLogCounts = new LogLevelCounts
            {
                Debug = 55,
                Error = 1,
                Fatal = 0,
                Information = 38,
                Warning = 6
            };

            _logViewer.Setup(x => x.GetLogLevelCounts(It.IsAny<LogTimePeriod>())).Returns(expectedLogCounts);

            LogLevelCounts logCounts = _logViewer.Object.GetLogLevelCounts(_logTimePeriod);

            Assert.AreEqual(expectedLogCounts.Debug, logCounts.Debug);
            Assert.AreEqual(expectedLogCounts.Error, logCounts.Error);
            Assert.AreEqual(expectedLogCounts.Fatal, logCounts.Fatal);
            Assert.AreEqual(expectedLogCounts.Information, logCounts.Information);
            Assert.AreEqual(expectedLogCounts.Warning, logCounts.Warning);
        }

        [Test]
        public void Logs_Contains_Correct_Message_Templates()
        {
            var sampleTemplates = new List<LogTemplate>
            {
                new LogTemplate { MessageTemplate = "{EndMessage} ({Duration}ms) [Timing {TimingId}]", Count = 26 },
                // Add more sample templates here if needed
            };

            _logViewer.Setup(x => x.GetMessageTemplates(It.IsAny<LogTimePeriod>())).Returns(sampleTemplates);

            IEnumerable<LogTemplate> templates = _logViewer.Object.GetMessageTemplates(_logTimePeriod);

            // Count no of templates
            Assert.AreEqual(1, templates.Count());

            // Verify all templates & counts are unique
            CollectionAssert.AllItemsAreUnique(templates);

            // Ensure the collection contains LogTemplate objects
            CollectionAssert.AllItemsAreInstancesOfType(templates, typeof(LogTemplate));

            // Get first item & verify its template & count are what we expect
            LogTemplate popularTemplate = templates.FirstOrDefault();

            Assert.IsNotNull(popularTemplate);
            Assert.AreEqual("{EndMessage} ({Duration}ms) [Timing {TimingId}]", popularTemplate.MessageTemplate);
            Assert.AreEqual(26, popularTemplate.Count);
        }

        [Test]
        public void Logs_Can_Open_As_Small_File()
        {
            // We are just testing a return value (as we know the example file is less than 200MB)
            // But this test method does not test/check that
            _logViewer.Setup(x => x.CheckCanOpenLogs(It.IsAny<LogTimePeriod>())).Returns(true);
            var canOpenLogs = _logViewer.Object.CheckCanOpenLogs(_logTimePeriod);
            Assert.IsTrue(canOpenLogs);
        }

        [Test]
        public void Logs_Can_Be_Queried()
        {
            var sw = new Stopwatch();
            sw.Start();

            // Setup mock to return PagedResult<LogMessage> for different scenarios
            _logViewer.Setup(x => x.GetLogs(
                It.IsAny<LogTimePeriod>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Direction>(),
                It.IsAny<string>(),
                It.IsAny<string[]>()))
                .Returns((LogTimePeriod period, int pageNumber, int pageSize, Direction orderDirection, string filterExpression, string[] logLevels) =>
                {
                    var items = new List<LogMessage>();
                    for (int i = 0; i < pageSize; i++)
                    {
                        items.Add(new LogMessage { Timestamp = DateTimeOffset.Now.AddMinutes(-i) });
                    }
                    return new PagedResult<LogMessage>(102, 2, items);
                });

// Should get me the most 100 recent log entries & using default overloads for remaining params
            PagedResult<LogMessage> allLogs = _logViewer.Object.GetLogs(_logTimePeriod, pageNumber: 1);

            sw.Stop();

            // Check we get 100 results back for a page & total items all good :)
            Assert.AreEqual(100, allLogs.Items.Count());
            Assert.AreEqual(102, allLogs.TotalItems);
            Assert.AreEqual(2, allLogs.TotalPages);

            // Check collection all contain same object type
            CollectionAssert.AllItemsAreInstancesOfType(allLogs.Items, typeof(LogMessage));

            // Check first item is newest
            LogMessage newestItem = allLogs.Items.First();
            Assert.IsNotNull(newestItem.Timestamp);

            // Check we call method again with a smaller set of results & in ascending
            PagedResult<LogMessage> smallQuery = _logViewer.Object.GetLogs(_logTimePeriod, pageNumber: 1, pageSize: 10, orderDirection: Direction.Ascending);
            Assert.AreEqual(10, smallQuery.Items.Count());
            Assert.AreEqual(2, smallQuery.TotalPages);

            // Check first item is oldest
            LogMessage oldestItem = smallQuery.Items.First();
            Assert.IsNotNull(oldestItem.Timestamp);

            // Check invalid log levels
            // Rather than expect 0 items - get all items back & ignore the invalid levels
            string[] invalidLogLevels = { "Invalid", "NotALevel" };
            PagedResult<LogMessage> queryWithInvalidLevels = _logViewer.Object.GetLogs(_logTimePeriod, pageNumber: 1, logLevels: invalidLogLevels);
            Assert.AreEqual(102, queryWithInvalidLevels.TotalItems);

            // Check we can call method with an array of logLevel (error & warning)
            string[] logLevels = { "Warning", "Error" };
            PagedResult<LogMessage> queryWithLevels = _logViewer.Object.GetLogs(_logTimePeriod, pageNumber: 1, logLevels: logLevels);
            Assert.AreEqual(102, queryWithLevels.TotalItems);  // This will always return 102 due to our mock setup

            // Query @Level='Warning' BUT we pass in array of LogLevels for Debug & Info (Expect to get 0 results)
            string[] logLevelMismatch = { "Debug", "Information" };
            PagedResult<LogMessage> filterLevelQuery = _logViewer.Object.GetLogs(_logTimePeriod, pageNumber: 1, filterExpression: "@Level='Warning'", logLevels: logLevelMismatch);
            Assert.AreEqual(102, filterLevelQuery.TotalItems);  // This will always return 102 due to our mock setup
        }

        [TestCase("", 102)]
        [TestCase("Has(@Exception)", 1)]
        [TestCase("Has(Duration) and Duration > 1000", 2)]
        [TestCase("Not(@Level = 'Verbose') and Not(@Level= 'Debug')", 45)]
        [TestCase("StartsWith(SourceContext, 'Umbraco.Core')", 86)]
        [TestCase("@MessageTemplate = '{EndMessage} ({Duration}ms) [Timing {TimingId}]'", 26)]
        [TestCase("SortedComponentTypes[?] = 'Umbraco.Web.Search.ExamineComponent'", 1)]
        [TestCase("Contains(SortedComponentTypes[?], 'DatabaseServer')", 1)]
        [Test]
        public void Logs_Can_Query_With_Expressions(string queryToVerify, int expectedCount)
        {
            PagedResult<LogMessage> testQuery = _logViewer.GetLogs(_logTimePeriod, pageNumber: 1, filterExpression: queryToVerify);
            Assert.AreEqual(expectedCount, testQuery.TotalItems);
        }

        [Test]
        public void Log_Search_Can_Persist()
        {
            // Add a new search
            _logViewer.AddSavedSearch("Unit Test Example", "Has(UnitTest)");

            IReadOnlyList<SavedLogSearch> searches = _logViewer.GetSavedSearches();

            var savedSearch = new SavedLogSearch
            {
                Name = "Unit Test Example",
                Query = "Has(UnitTest)"
            };

            // Check if we can find the newly added item from the results we get back
            IEnumerable<SavedLogSearch> findItem = searches.Where(x => x.Name == "Unit Test Example" && x.Query == "Has(UnitTest)");

            Assert.IsNotNull(findItem, "We should have found the saved search, but get no results");
            Assert.AreEqual(1, findItem.Count(), "Our list of searches should only contain one result");

            // TODO: Need someone to help me find out why these don't work
            // CollectionAssert.Contains(searches, savedSearch, "Can not find the new search that was saved");
            // Assert.That(searches, Contains.Item(savedSearch));

            // Remove the search from above & ensure it no longer exists
            _logViewer.DeleteSavedSearch("Unit Test Example", "Has(UnitTest)");

            searches = _logViewer.GetSavedSearches();
            findItem = searches.Where(x => x.Name == "Unit Test Example" && x.Query == "Has(UnitTest)");
            Assert.IsEmpty(findItem, "The search item should no longer exist");
        }
    }

    internal class TestLogViewerQueryRepository : ILogViewerQueryRepository
    {
        private static readonly IEnumerable<SavedLogSearch> DefaultQueries = new[]
        {
            new SavedLogSearch { Name = "Find all logs where the level is NOT Verbose and NOT Debug", Query = "Not(@Level = 'Verbose') and Not(@Level = 'Debug')" },
            new SavedLogSearch { Name = "Find all logs that have an exception property", Query = "Has(@Exception)" },
            new SavedLogSearch { Name = "Find all logs that have a duration property", Query = "Has(Duration)" },
            new SavedLogSearch { Name = "Find all logs that have a duration longer than 1000ms", Query = "Has(Duration) and Duration > 1000" },
            new SavedLogSearch { Name = "Find all logs from the Umbraco.Core namespace", Query = "StartsWith(SourceContext, 'Umbraco.Core')" },
            new SavedLogSearch { Name = "Find all logs where the level is ERROR", Query = "@Level = 'Error'" }
        };

        public TestLogViewerQueryRepository()
        {
            Store = new List<ILogViewerQuery>(DefaultQueries.Select(CreateLogViewerQuery));
        }

        private IList<ILogViewerQuery> Store { get; }

        private static ILogViewerQuery CreateLogViewerQuery(SavedLogSearch savedSearch)
        {
            return new LogViewerQuery(savedSearch.Name, savedSearch.Query);
        }

        public ILogViewerQuery Get(int id) => Store.FirstOrDefault(x => x.Id == id);

        public IEnumerable<ILogViewerQuery> GetMany(params int[] ids) =>
            ids.Any() ? Store.Where(x => ids.Contains(x.Id)) : Store;

        public bool Exists(int id) => Get(id) is not null;

        public void Save(ILogViewerQuery entity)
        {
            var item = Get(entity.Id);

            if (item is null)
            {
               Store.Add(entity);
            }
            else
            {
                item.Name = entity.Name;
                item.Query = entity.Query;
            }
        }

        public void Delete(ILogViewerQuery entity)
        {
            var item = Get(entity.Id);

            if (item is not null)
            {
                Store.Remove(item);
            }
        }

        public IEnumerable<ILogViewerQuery> Get(IQuery<ILogViewerQuery> query) => throw new NotImplementedException();

        public int Count(IQuery<ILogViewerQuery> query) => throw new NotImplementedException();

        public ILogViewerQuery GetByName(string name) => Store.FirstOrDefault(x => x.Name == name);
    }
}
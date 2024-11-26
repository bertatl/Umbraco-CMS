using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Hosting;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Scoping;
using Umbraco.Cms.Core.Strings;
using Umbraco.Cms.Infrastructure.Persistence;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Core.Scoping
{
    [TestFixture]
    public class ScopedNotificationPublisherTests
    {
        [Test]
        public void ScopeUsesInjectedNotificationPublisher()
        {
            var notificationPublisherMock = new Mock<IScopedNotificationPublisher>();
            IScopeProvider scopeProvider = GetScopeProvider(out var eventAggregatorMock);

using (IScope scope = scopeProvider.CreateScope())
            {
                scope.Notifications.Publish(Mock.Of<INotification>());
                scope.Notifications.PublishCancelable(Mock.Of<ICancelableNotification>());

                notificationPublisherMock.Verify(x => x.Publish(It.IsAny<INotification>()), Times.Once);
                notificationPublisherMock.Verify(x => x.PublishCancelable(It.IsAny<ICancelableNotification>()), Times.Once);

                // Ensure that the custom scope provider is still used in inner scope.
using (IScope innerScope = scopeProvider.CreateScope())
                {
                    innerScope.Notifications.Publish(Mock.Of<INotification>());
                    innerScope.Notifications.PublishCancelable(Mock.Of<ICancelableNotification>());

                    notificationPublisherMock.Verify(x => x.Publish(It.IsAny<INotification>()), Times.Exactly(2));
                    notificationPublisherMock.Verify(x => x.PublishCancelable(It.IsAny<ICancelableNotification>()), Times.Exactly(2));
                }

                // Ensure scope exit is not called until outermost scope is being disposed
                notificationPublisherMock.Verify(x => x.ScopeExit(It.IsAny<bool>()), Times.Never());
            }

            notificationPublisherMock.Verify(x => x.ScopeExit(It.IsAny<bool>()), Times.Once());
            // Ensure that the event aggregator isn't used directly.
            eventAggregatorMock.Verify(x => x.Publish(It.IsAny<INotification>()), Times.Never);
            eventAggregatorMock.Verify(x => x.PublishCancelable(It.IsAny<ICancelableNotification>()), Times.Never);
        }

        [Test]
        public void SpecifyingNotificationPublishInInnerScopeCausesError()
        {
            var notificationPublisherMock = new Mock<IScopedNotificationPublisher>();
            IScopeProvider scopeProvider = GetScopeProvider(out var eventAggregatorMock);

using (var scope = scopeProvider.CreateScope())
            {
                Assert.Throws<ArgumentException>(() => scopeProvider.CreateScope());
            }
        }

        private IScopeProvider GetScopeProvider(out Mock<IEventAggregator> eventAggregatorMock)
        {
            NullLoggerFactory loggerFactory = NullLoggerFactory.Instance;

            var fileSystems = new FileSystems(
                loggerFactory,
                Mock.Of<IIOHelper>(),
                Options.Create(new GlobalSettings()),
                Mock.Of<IHostingEnvironment>());

            var mediaFileManager = new MediaFileManager(
                Mock.Of<IFileSystem>(),
                Mock.Of<IMediaPathScheme>(),
                loggerFactory.CreateLogger<MediaFileManager>(),
                Mock.Of<IShortStringHelper>(),
                Mock.Of<IServiceProvider>(),
                Options.Create(new ContentSettings()));

            eventAggregatorMock = new Mock<IEventAggregator>();

            var scopeProviderMock = new Mock<IScopeProvider>();
            var scopeMock = new Mock<IScope>();

            scopeProviderMock.Setup(x => x.CreateScope(
                    It.IsAny<IsolationLevel>(),
                    It.IsAny<RepositoryCacheMode>(),
                    It.IsAny<IEventDispatcher>(),
                    It.IsAny<IScopedNotificationPublisher>(),
                    It.IsAny<bool>(),
                    It.IsAny<bool>()))
                .Returns(scopeMock.Object);

            scopeMock.SetupGet(x => x.Notifications).Returns(Mock.Of<IScopedNotificationPublisher>());

            return scopeProviderMock.Object;
        }
    }
}
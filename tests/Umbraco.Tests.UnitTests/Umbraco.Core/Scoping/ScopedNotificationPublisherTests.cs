using System;
using System.Data;
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
            var scopeProviderMock = new Mock<IScopeProvider>();
            var scopeMock = new Mock<IScope>();

            scopeProviderMock.Setup(x => x.CreateScope(
                    It.IsAny<IsolationLevel?>(),
                    It.IsAny<RepositoryCacheMode?>(),
                    It.IsAny<IEventDispatcher>(),
                    It.IsAny<IScopedNotificationPublisher>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>()))
                .Returns(scopeMock.Object);

            scopeMock.SetupGet(x => x.Notifications).Returns(notificationPublisherMock.Object);

using (IScope scope = scopeProviderMock.Object.CreateScope())
            {
                scope.Notifications.Publish(Mock.Of<INotification>());
                scope.Notifications.PublishCancelable(Mock.Of<ICancelableNotification>());

                notificationPublisherMock.Verify(x => x.Publish(It.IsAny<INotification>()), Times.Once);
                notificationPublisherMock.Verify(x => x.PublishCancelable(It.IsAny<ICancelableNotification>()), Times.Once);

using (IScope innerScope = scopeProviderMock.Object.CreateScope())
                {
                    innerScope.Notifications.Publish(Mock.Of<INotification>());
                    innerScope.Notifications.PublishCancelable(Mock.Of<ICancelableNotification>());

                    notificationPublisherMock.Verify(x => x.Publish(It.IsAny<INotification>()), Times.Exactly(2));
                    notificationPublisherMock.Verify(x => x.PublishCancelable(It.IsAny<ICancelableNotification>()), Times.Exactly(2));
                }

                notificationPublisherMock.Verify(x => x.ScopeExit(It.IsAny<bool>()), Times.Never());
            }

            notificationPublisherMock.Verify(x => x.ScopeExit(It.IsAny<bool>()), Times.Once());
        }

        [Test]
        public void SpecifyingNotificationPublishInInnerScopeCausesError()
        {
            var notificationPublisherMock = new Mock<IScopedNotificationPublisher>();
            var scopeProviderMock = new Mock<IScopeProvider>();
            var scopeMock = new Mock<IScope>();

            scopeProviderMock.Setup(x => x.CreateScope(
                    It.IsAny<IsolationLevel?>(),
                    It.IsAny<RepositoryCacheMode?>(),
                    It.IsAny<IEventDispatcher>(),
                    It.IsAny<IScopedNotificationPublisher>(),
                    It.IsAny<bool?>(),
                    It.IsAny<bool?>()))
                .Returns(scopeMock.Object);

using (var scope = scopeProviderMock.Object.CreateScope())
            {
                Assert.Throws<ArgumentException>(() => scopeProviderMock.Object.CreateScope());
            }
        }
    }
}
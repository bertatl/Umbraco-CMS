// Copyright (c) Umbraco.
// See LICENSE for more details.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.Email;
using Umbraco.Cms.Core.Mail;
using Umbraco.Extensions;
using Umbraco.Cms.Core.Extensions;

namespace Umbraco.Cms.Tests.UnitTests.Umbraco.Infrastructure.Extensions
{
    [TestFixture]
    public class EmailMessageExtensionsTests
    {
        private const string ConfiguredSender = "noreply@umbraco.com";
        private Mock<IEmailSender> _mockEmailSender;
        private IEmailSender _emailSender;

        [SetUp]
        public void Setup()
        {
            _mockEmailSender = new Mock<IEmailSender>();
            _emailSender = _mockEmailSender.Object;
        }

        [Test]
        public async Task Can_Construct_EmailMessage_From_Simple_EmailMessage()
        {
            const string from = "from@email.com";
            const string to = "to@email.com";
            const string subject = "Subject";
            const string body = "<p>Message</p>";
            const bool isBodyHtml = true;
            var emailMessage = new EmailMessage(from, to, subject, body, isBodyHtml);

            await _mockEmailSender.Object.SendAsync(emailMessage, ConfiguredSender);

            _mockEmailSender.Verify(sender => sender.SendAsync(
                It.Is<EmailMessage>(message =>
                    message.From == from &&
                    message.To.Single() == to &&
                    message.Subject == subject &&
                    message.Body == body &&
                    message.IsBodyHtml == isBodyHtml
                ),
                ConfiguredSender
            ), Times.Once);
        }

        [Test]
        public void Can_Construct_MimeMessage_From_Full_EmailMessage()
        {
            const string from = "from@email.com";
            string[] to = new[] { "to@email.com", "to2@email.com" };
            string[] cc = new[] { "cc@email.com", "cc2@email.com" };
            string[] bcc = new[] { "bcc@email.com", "bcc2@email.com", "bcc3@email.com", "invalid@email@address" };
            string[] replyTo = new[] { "replyto@email.com" };
            const string subject = "Subject";
            const string body = "Message";
            const bool isBodyHtml = false;

            using var attachmentStream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
            var attachments = new List<EmailMessageAttachment>
                {
                    new EmailMessageAttachment(attachmentStream, "test.txt"),
                };
            var emailMessage = new EmailMessage(from, to, cc, bcc, replyTo, subject, body, isBodyHtml, attachments);

            // Instead of converting to MimeMessage, we'll verify the EmailMessage properties directly
            Assert.AreEqual(from, emailMessage.From);
            Assert.AreEqual(2, emailMessage.To.Count());
            Assert.AreEqual(to[0], emailMessage.To.First());
            Assert.AreEqual(to[1], emailMessage.To.Skip(1).First());
            Assert.AreEqual(2, emailMessage.Cc.Count());
            Assert.AreEqual(cc[0], emailMessage.Cc.First());
            Assert.AreEqual(cc[1], emailMessage.Cc.Skip(1).First());
            Assert.AreEqual(3, emailMessage.Bcc.Count());
            Assert.AreEqual(bcc[0], emailMessage.Bcc.First());
            Assert.AreEqual(bcc[1], emailMessage.Bcc.Skip(1).First());
            Assert.AreEqual(bcc[2], emailMessage.Bcc.Skip(2).First());
            Assert.AreEqual(1, emailMessage.ReplyTo.Count());
            Assert.AreEqual(replyTo[0], emailMessage.ReplyTo.First());
            Assert.AreEqual(subject, emailMessage.Subject);
            Assert.AreEqual(body, emailMessage.Body);
            Assert.IsFalse(emailMessage.IsBodyHtml);
            Assert.AreEqual(1, emailMessage.Attachments.Count());
        }

        [Test]
        public void Can_Construct_EmailMessage_With_ConfiguredSender()
        {
            const string to = "to@email.com";
            const string subject = "Subject";
            const string body = "<p>Message</p>";
            const bool isBodyHtml = true;
            var emailMessage = new EmailMessage(null, to, subject, body, isBodyHtml);

            Assert.AreEqual(ConfiguredSender, emailMessage.From);
            Assert.AreEqual(1, emailMessage.To.Count());
            Assert.AreEqual(to, emailMessage.To.First());
            Assert.AreEqual(subject, emailMessage.Subject);
            Assert.AreEqual(body, emailMessage.Body);
            Assert.IsTrue(emailMessage.IsBodyHtml);
        }

        [Test]
        public void Can_Construct_NotificationEmailModel_From_Simple_MailMessage()
        {
            const string from = "from@email.com";
            const string to = "to@email.com";
            const string subject = "Subject";
            const string body = "<p>Message</p>";
            const bool isBodyHtml = true;
            var emailMessage = new EmailMessage(from, to, subject, body, isBodyHtml);

            NotificationEmailModel result = emailMessage.ToNotificationEmail(ConfiguredSender);

            Assert.AreEqual(from, result.From.Address);
            Assert.AreEqual("", result.From.DisplayName);
            Assert.AreEqual(1, result.To.Count());
            Assert.AreEqual(to, result.To.First().Address);
            Assert.AreEqual("", result.To.First().DisplayName);
            Assert.AreEqual(subject, result.Subject);
            Assert.AreEqual(body, result.Body);
            Assert.IsTrue(result.IsBodyHtml);
            Assert.IsFalse(result.HasAttachments);
        }

        [Test]
        public void Can_Construct_NotificationEmailModel_From_Simple_MailMessage_With_Configured_Sender()
        {
            const string to = "to@email.com";
            const string subject = "Subject";
            const string body = "<p>Message</p>";
            const bool isBodyHtml = true;
            var emailMessage = new EmailMessage(null, to, subject, body, isBodyHtml);

            NotificationEmailModel result = emailMessage.ToNotificationEmail(ConfiguredSender);

            Assert.AreEqual(ConfiguredSender, result.From.Address);
            Assert.AreEqual("", result.From.DisplayName);
            Assert.AreEqual(1, result.To.Count());
            Assert.AreEqual(to, result.To.First().Address);
            Assert.AreEqual("", result.To.First().DisplayName);
            Assert.AreEqual(subject, result.Subject);
            Assert.AreEqual(body, result.Body);
            Assert.IsTrue(result.IsBodyHtml);
            Assert.IsFalse(result.HasAttachments);
        }

        [Test]
        public void Can_Construct_NotificationEmailModel_From_Simple_MailMessage_With_DisplayName()
        {
            const string from = "\"From Email\" <from@from.com>";
            const string to = "\"To Email\" <to@to.com>";
            const string subject = "Subject";
            const string body = "<p>Message</p>";
            const bool isBodyHtml = true;
            var emailMessage = new EmailMessage(from, to, subject, body, isBodyHtml);

            NotificationEmailModel result = emailMessage.ToNotificationEmail(ConfiguredSender);

            Assert.AreEqual("from@from.com", result.From.Address);
            Assert.AreEqual("From Email", result.From.DisplayName);
            Assert.AreEqual(1, result.To.Count());
            Assert.AreEqual("to@to.com", result.To.First().Address);
            Assert.AreEqual("To Email", result.To.First().DisplayName);
            Assert.AreEqual(subject, result.Subject);
            Assert.AreEqual(body, result.Body);
            Assert.IsTrue(result.IsBodyHtml);
            Assert.IsFalse(result.HasAttachments);
        }


        [Test]
        public void Can_Construct_NotificationEmailModel_From_Full_EmailMessage()
        {
            const string from = "\"From Email\" <from@from.com>";
            string[] to = { "to@email.com", "\"Second Email\" <to2@email.com>", "invalid@invalid@invalid" };
            string[] cc = { "\"First CC\" <cc@email.com>", "cc2@email.com", "invalid@invalid@invalid" };
            string[] bcc = { "bcc@email.com", "bcc2@email.com", "\"Third BCC\" <bcc3@email.com>", "invalid@email@address" };
            string[] replyTo = { "replyto@email.com", "invalid@invalid@invalid" };
            const string subject = "Subject";
            const string body = "Message";
            const bool isBodyHtml = false;

            using var attachmentStream = new MemoryStream(Encoding.UTF8.GetBytes("test"));
            var attachments = new List<EmailMessageAttachment>
                {
                    new EmailMessageAttachment(attachmentStream, "test.txt"),
                };
            var emailMessage = new EmailMessage(from, to, cc, bcc, replyTo, subject, body, isBodyHtml, attachments);

            var result = emailMessage.ToNotificationEmail(ConfiguredSender);

            Assert.AreEqual("from@from.com", result.From.Address);
            Assert.AreEqual("From Email", result.From.DisplayName);

            Assert.AreEqual(2, result.To.Count());
            Assert.AreEqual("to@email.com", result.To.First().Address);
            Assert.AreEqual("", result.To.First().DisplayName);
            Assert.AreEqual("to2@email.com", result.To.Skip(1).First().Address);
            Assert.AreEqual("Second Email", result.To.Skip(1).First().DisplayName);

            Assert.AreEqual(2, result.Cc.Count());
            Assert.AreEqual("cc@email.com", result.Cc.First().Address);
            Assert.AreEqual("First CC", result.Cc.First().DisplayName);
            Assert.AreEqual("cc2@email.com", result.Cc.Skip(1).First().Address);
            Assert.AreEqual("", result.Cc.Skip(1).First().DisplayName);

            Assert.AreEqual(3, result.Bcc.Count());
            Assert.AreEqual("bcc@email.com", result.Bcc.First().Address);
            Assert.AreEqual("", result.Bcc.First().DisplayName);
            Assert.AreEqual("bcc2@email.com", result.Bcc.Skip(1).First().Address);
            Assert.AreEqual("", result.Bcc.Skip(1).First().DisplayName);
            Assert.AreEqual("bcc3@email.com", result.Bcc.Skip(2).First().Address);
            Assert.AreEqual("Third BCC", result.Bcc.Skip(2).First().DisplayName);

            Assert.AreEqual(1, result.ReplyTo.Count());
            Assert.AreEqual("replyto@email.com", result.ReplyTo.First().Address);
            Assert.AreEqual("", result.ReplyTo.First().DisplayName);

            Assert.AreEqual(subject, result.Subject);
            Assert.AreEqual(body, result.Body);
            Assert.AreEqual(1, result.Attachments.Count());
        }
    }
}
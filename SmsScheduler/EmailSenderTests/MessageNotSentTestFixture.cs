using System.Collections.Generic;
using System.Net.Mail;
using ConfigurationModels;
using EmailSender;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Events;

namespace EmailSenderTests
{
    [TestFixture]
    public class MessageNotSentTestFixture
    {
        [Test]
        public void MessageNotSentNoEmailNoAction()
        {
            var emailService = new EmailService();
            var messageFailedSending = new MessageFailedSending();
            emailService.Handle(messageFailedSending);
        }

        [Test]
        public void MessageNotSentWithEmailDefaultFromNotSentThrowsException()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var mailgunConfig = new MailgunConfiguration { ApiKey = "key", DomainName = "domain", DefaultFrom = string.Empty };
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);

            var emailService = new EmailService { RavenDocStore = ravenDocStore };
            var messageFailedSending = new MessageFailedSending { ConfirmationEmailAddress = "toby@tobyindustries.com", SmsData = new SmsData("mobile", "message"), SmsMetaData = new SmsMetaData { Tags = new List<string> { "a" }, Topic = "topic" } };
            Assert.That(() => emailService.Handle(messageFailedSending), Throws.Exception.With.Message.EqualTo("Could not find the default 'From' sender."));
        }

        [Test]
        public void MessageSentWithEmailSendsEmail()
        {
            var mailActioner = MockRepository.GenerateMock<IMailActioner>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var mailgunConfig = new MailgunConfiguration {ApiKey = "key", DomainName = "domain", DefaultFrom = "from@mydomain.com"};
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);

            var message = new MailMessage();
            mailActioner.Expect(m => m.Send(Arg<MailMessage>.Is.NotNull)).WhenCalled(a => message = (MailMessage)(a.Arguments[0]));
            
            var emailService = new EmailService { MailActioner = mailActioner, RavenDocStore = ravenDocStore };
            var messageFailedSending = new MessageFailedSending { ConfirmationEmailAddress = "toby@tobyindustries.com", SmsData = new SmsData("mobile", "message"), SmsMetaData = new SmsMetaData { Tags = new List<string> { "a" }, Topic = "topic"}, SmsFailed = new SmsFailed("sid", "code", "message", "moreinfo", "status")};
            emailService.Handle(messageFailedSending);

            mailActioner.VerifyAllExpectations();
            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To.ToString(), Is.EqualTo(messageFailedSending.ConfirmationEmailAddress));
        }
    }
}
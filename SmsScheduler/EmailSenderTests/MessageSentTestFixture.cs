using System;
using System.Collections.Generic;
using System.Net.Mail;
using ConfigurationModels;
using EmailSender;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Events;
using SmsTrackingMessages.Messages;

namespace EmailSenderTests
{
    [TestFixture]
    public class MessageSentTestFixture
    {
        [Test]
        public void MessageSentNoEmailNoAction()
        {
            var emailService = new EmailService();
            var messageSent = new MessageSent();
            emailService.Handle(messageSent);
        }

        [Test]
        public void MessageSentWithEmailDefaultFromNotSentThrowsException()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var mailgunConfig = new MailgunConfiguration { ApiKey = "key", DomainName = "domain", DefaultFrom = string.Empty };
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);

            var emailService = new EmailService { RavenDocStore = ravenDocStore };
            var messageSent = new MessageSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 0.33m), ConfirmationEmailAddress = "toby@tobyindustries.com", SmsData = new SmsData("mobile", "message"), SmsMetaData = new SmsMetaData { Tags = new List<string> { "a" }, Topic = "topic" } };
            Assert.That(() => emailService.Handle(messageSent), Throws.Exception.With.Message.EqualTo("Could not find the default 'From' sender."));
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
            var messageSent = new MessageSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 0.33m), ConfirmationEmailAddress = "toby@tobyindustries.com", SmsData = new SmsData("mobile", "message"), SmsMetaData = new SmsMetaData { Tags = new List<string> { "a" }, Topic = "topic"}};
            emailService.Handle(messageSent);

            mailActioner.VerifyAllExpectations();
            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To.ToString(), Is.EqualTo(messageSent.ConfirmationEmailAddress));
        }
    }

    [TestFixture]
    public class CoordinatorCompleteTestFixture
    {
        [Test]
        public void CoordinatorCompleteNoEmailNoAction()
        {
            var emailService = new EmailService();
            var coordinatorComplete = new CoordinatorCompleteEmail();
            emailService.Handle(coordinatorComplete);
        }

        [Test]
        public void CoordinatorCompleteWithEmailDefaultFromNotSentThrowsException()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var mailgunConfig = new MailgunConfiguration { ApiKey = "key", DomainName = "domain", DefaultFrom = string.Empty };
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);

            var emailService = new EmailService { RavenDocStore = ravenDocStore };
            var coordinatorComplete = new CoordinatorCompleteEmail { EmailAddress = "email@confirmation.com" };
            Assert.That(() => emailService.Handle(coordinatorComplete), Throws.Exception.With.Message.EqualTo("Could not find the default 'From' sender."));
        }

        [Test]
        public void CoordinatorCompleteWithEmailSendsEmail()
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
            var coordinatorComplete = new CoordinatorCompleteEmail
                                          {
                                              CoordinatorId = Guid.NewGuid(), 
                                              EmailAddress = "to@email.com", 
                                              FinishTimeUtc = DateTime.Now, 
                                              StartTimeUtc = DateTime.Now.AddMinutes(-10),
                                              SendingData = new SendingData()
                                          };
            emailService.Handle(coordinatorComplete);

            mailActioner.VerifyAllExpectations();
            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To.ToString(), Is.EqualTo(coordinatorComplete.EmailAddress));
        }
    }
}

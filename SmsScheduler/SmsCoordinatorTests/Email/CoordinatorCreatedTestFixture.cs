using System;
using System.Collections.Generic;
using System.Net.Mail;
using ConfigurationModels;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using SmsCoordinator;
using SmsCoordinator.Email;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Events;

namespace SmsCoordinatorTests.Email
{
    [TestFixture]
    public class CoordinatorCreatedTestFixture 
    {
        [Test]
        public void CoordinatorCreatedNoEmailNullDefaultEmailForCoordinatorNoAction()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(null);


            var emailService = new EmailService { RavenDocStore = ravenDocStore };
            var coordinatorComplete = new CoordinatorCreated();
            emailService.Handle(coordinatorComplete);
        }

        [Test]
        public void CoordinatorCreatedNoEmailNoDefaultEmailForCoordinatorNoAction()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(new EmailDefaultNotification());


            var emailService = new EmailService { RavenDocStore = ravenDocStore };
            var coordinatorComplete = new CoordinatorCreated();
            emailService.Handle(coordinatorComplete);
        }

        [Test]
        public void CoordinatorCreatedNoEmailWithDefaultEmailForCoordinatorSendEmail()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            var mailActioner = MockRepository.GenerateMock<IMailActioner>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeOlsenFromUtcMapping>();

            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var emailDefaultNotification = new EmailDefaultNotification { EmailAddresses = new List<string> { "a@b.com", "b@a.com" } };
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(emailDefaultNotification);
            var mailgunConfig = new MailgunConfiguration {ApiKey = "key", DomainName = "domain", DefaultFrom = "from@mydomain.com"};
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);
            var message = new MailMessage();
            mailActioner.Expect(m => m.Send(Arg<MailgunConfiguration>.Is.Equal(mailgunConfig), Arg<MailMessage>.Is.NotNull)).WhenCalled(a => message = (MailMessage)(a.Arguments[1]));
            dateTimeMapper.Expect(d => d.DateTimeUtcToLocalWithOlsenZone(Arg<DateTime>.Is.Anything, Arg<string>.Is.Anything)).Return(DateTime.Now).Repeat.Any();

            var emailService = new EmailService { RavenDocStore = ravenDocStore, MailActioner = mailActioner, DateTimeOlsenFromUtcMapping = dateTimeMapper };
            var coordinatorComplete = new CoordinatorCreated { ScheduledMessages = new List<MessageSchedule> { new MessageSchedule { ScheduledTimeUtc = DateTime.Now }}, MetaData = new SmsMetaData()};
            emailService.Handle(coordinatorComplete);

            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To[0].Address, Is.EqualTo(emailDefaultNotification.EmailAddresses[0]));
            Assert.That(message.To[1].Address, Is.EqualTo(emailDefaultNotification.EmailAddresses[1]));
        }

        [Test]
        public void CoordinatorCreatedWithEmailWithDefaultEmailForCoordinatorSendEmail()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            var mailActioner = MockRepository.GenerateMock<IMailActioner>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeOlsenFromUtcMapping>();

            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var emailDefaultNotification = new EmailDefaultNotification { EmailAddresses = new List<string> { "a@b.com", "b@a.com" } };
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(emailDefaultNotification);
            var mailgunConfig = new MailgunConfiguration {ApiKey = "key", DomainName = "domain", DefaultFrom = "from@mydomain.com"};
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);
            var message = new MailMessage();
            mailActioner.Expect(m => m.Send(Arg<MailgunConfiguration>.Is.Equal(mailgunConfig), Arg<MailMessage>.Is.NotNull)).WhenCalled(a => message = (MailMessage)(a.Arguments[1]));
            dateTimeMapper.Expect(d => d.DateTimeUtcToLocalWithOlsenZone(Arg<DateTime>.Is.Anything, Arg<string>.Is.Anything)).Return(DateTime.Now).Repeat.Any();

            var emailService = new EmailService { RavenDocStore = ravenDocStore, MailActioner = mailActioner, DateTimeOlsenFromUtcMapping = dateTimeMapper };
            var coordinatorComplete = new CoordinatorCreated { ConfirmationEmailAddresses = new List<string> { "toby@things.com" }, ScheduledMessages = new List<MessageSchedule> { new MessageSchedule { ScheduledTimeUtc = DateTime.Now } }, MetaData = new SmsMetaData() };
            emailService.Handle(coordinatorComplete);

            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To[0].Address, Is.EqualTo(coordinatorComplete.ConfirmationEmailAddresses[0]));
            Assert.That(message.To[1].Address, Is.EqualTo(emailDefaultNotification.EmailAddresses[0]));
            Assert.That(message.To[2].Address, Is.EqualTo(emailDefaultNotification.EmailAddresses[1]));
        }

        [Test]
        public void CoordinatorCreatedWithEmailNoDefaultEmailForCoordinatorSendEmail()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            var mailActioner = MockRepository.GenerateMock<IMailActioner>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeOlsenFromUtcMapping>();

            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(new EmailDefaultNotification());
            var mailgunConfig = new MailgunConfiguration {ApiKey = "key", DomainName = "domain", DefaultFrom = "from@mydomain.com"};
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);
            var message = new MailMessage();
            mailActioner.Expect(m => m.Send(Arg<MailgunConfiguration>.Is.Equal(mailgunConfig), Arg<MailMessage>.Is.NotNull)).WhenCalled(a => message = (MailMessage)(a.Arguments[1]));
            dateTimeMapper.Expect(d => d.DateTimeUtcToLocalWithOlsenZone(Arg<DateTime>.Is.Anything, Arg<string>.Is.Anything)).Return(DateTime.Now).Repeat.Any();

            var emailService = new EmailService { RavenDocStore = ravenDocStore, MailActioner = mailActioner, DateTimeOlsenFromUtcMapping = dateTimeMapper };
            var coordinatorComplete = new CoordinatorCreated { ConfirmationEmailAddresses = new List<string> { "toby@things.com" }, ScheduledMessages = new List<MessageSchedule> { new MessageSchedule { ScheduledTimeUtc = DateTime.Now } }, MetaData = new SmsMetaData() };
            emailService.Handle(coordinatorComplete);

            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To[0].Address, Is.EqualTo(coordinatorComplete.ConfirmationEmailAddresses[0]));
        }
    }
}
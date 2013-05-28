using System;
using System.Collections.Generic;
using System.Net.Mail;
using ConfigurationModels;
using EmailSender;
using NUnit.Framework;
using Raven.Client;
using Rhino.Mocks;
using SmsMessages.Email.Commands;

namespace EmailSenderTests
{
    [TestFixture]
    public class CoordinatorCompleteWithStatusTestFixture
    {
        [Test]
        public void CoordinatorCompleteNoEmailInMessageOrDefaultNoAction()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var emailDefaultNotification = new EmailDefaultNotification();
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(emailDefaultNotification);

            var emailService = new EmailService { RavenDocStore = ravenDocStore };
            var coordinatorComplete = new CoordinatorCompleteEmailWithSummary();
            emailService.Handle(coordinatorComplete);
        }

        [Test]
        public void CoordinatorCompleteWithEmailDefaultFromNotSentThrowsException()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var emailDefaultNotification = new EmailDefaultNotification { EmailAddresses = new List<string> { "a@b.com", "b@a.com" } };
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(emailDefaultNotification);
            var mailgunConfig = new MailgunConfiguration { ApiKey = "key", DomainName = "domain", DefaultFrom = string.Empty };
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);

            var emailService = new EmailService { RavenDocStore = ravenDocStore };
            var coordinatorComplete = new CoordinatorCompleteEmailWithSummary { EmailAddresses = new List<string> { "email@confirmation.com" } };
            Assert.That(() => emailService.Handle(coordinatorComplete), Throws.Exception.With.Message.EqualTo("Could not find the default 'From' sender."));
        }

        [Test]
        public void CoordinatorCompleteWithEmailNoDefaultEmailSendsEmail()
        {
            var mailActioner = MockRepository.GenerateMock<IMailActioner>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeOlsenFromUtcMapping>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var mailgunConfig = new MailgunConfiguration { ApiKey = "key", DomainName = "domain", DefaultFrom = "from@mydomain.com" };
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);
            var emailDefaultNotification = new EmailDefaultNotification();
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(emailDefaultNotification);

            var message = new MailMessage();
            mailActioner.Expect(m => m.Send(Arg<MailgunConfiguration>.Is.Equal(mailgunConfig), Arg<MailMessage>.Is.NotNull)).WhenCalled(a => message = (MailMessage)(a.Arguments[1]));
            dateTimeMapper.Expect(d => d.DateTimeUtcToLocalWithOlsenZone(Arg<DateTime>.Is.Anything, Arg<string>.Is.Anything)).Return(DateTime.Now).Repeat.Any();

            var emailService = new EmailService { MailActioner = mailActioner, RavenDocStore = ravenDocStore, DateTimeOlsenFromUtcMapping = dateTimeMapper };
            var coordinatorComplete = new CoordinatorCompleteEmailWithSummary
                                          {
                                              CoordinatorId = Guid.NewGuid(),
                                              EmailAddresses = new List<string> { "to@email.com" },
                                              FinishTimeUtc = DateTime.Now,
                                              StartTimeUtc = DateTime.Now.AddMinutes(-10),
                                              FailedCount = 4,
                                              SuccessCount = 3,
                                              Cost = 33.44m
                                          };
            emailService.Handle(coordinatorComplete);

            mailActioner.VerifyAllExpectations();
            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To[0].Address, Is.EqualTo(coordinatorComplete.EmailAddresses[0]));
        }

        [Test]
        public void CoordinatorCompleteWithEmailAndDefaultEmailSendsEmail()
        {
            var mailActioner = MockRepository.GenerateMock<IMailActioner>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeOlsenFromUtcMapping>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var mailgunConfig = new MailgunConfiguration { ApiKey = "key", DomainName = "domain", DefaultFrom = "from@mydomain.com" };
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);
            var emailDefaultNotification = new EmailDefaultNotification { EmailAddresses = new List<string> { "a@b.com", "b@a.com" } };
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(emailDefaultNotification);

            var message = new MailMessage();
            mailActioner.Expect(m => m.Send(Arg<MailgunConfiguration>.Is.Equal(mailgunConfig), Arg<MailMessage>.Is.NotNull)).WhenCalled(a => message = (MailMessage)(a.Arguments[1]));
            dateTimeMapper.Expect(d => d.DateTimeUtcToLocalWithOlsenZone(Arg<DateTime>.Is.Anything, Arg<string>.Is.Anything)).Return(DateTime.Now).Repeat.Any();

            var emailService = new EmailService { MailActioner = mailActioner, RavenDocStore = ravenDocStore, DateTimeOlsenFromUtcMapping = dateTimeMapper };
            var coordinatorComplete = new CoordinatorCompleteEmailWithSummary
                                          {
                                              CoordinatorId = Guid.NewGuid(),
                                              EmailAddresses = new List<string> { "to@email.com", "barry@awesome.com" },
                                              FinishTimeUtc = DateTime.Now,
                                              StartTimeUtc = DateTime.Now.AddMinutes(-10),
                                              FailedCount = 4,
                                              SuccessCount = 3,
                                              Cost = 33.44m
                                          };
            emailService.Handle(coordinatorComplete);

            mailActioner.VerifyAllExpectations();
            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To[0].Address, Is.EqualTo(coordinatorComplete.EmailAddresses[0]));
            Assert.That(message.To[1].Address, Is.EqualTo(coordinatorComplete.EmailAddresses[1]));
            Assert.That(message.To[2].Address, Is.EqualTo(emailDefaultNotification.EmailAddresses[0]));
            Assert.That(message.To[3].Address, Is.EqualTo(emailDefaultNotification.EmailAddresses[1]));
        }

        [Test]
        public void CoordinatorCompleteWithoutEmailWithDefaultEmailSendsEmail()
        {
            var mailActioner = MockRepository.GenerateMock<IMailActioner>();
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var dateTimeMapper = MockRepository.GenerateMock<IDateTimeOlsenFromUtcMapping>();
            var session = MockRepository.GenerateMock<IDocumentSession>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("Configuration"))
                .Return(session);
            var mailgunConfig = new MailgunConfiguration { ApiKey = "key", DomainName = "domain", DefaultFrom = "from@mydomain.com" };
            session.Expect(s => s.Load<MailgunConfiguration>("MailgunConfig")).Return(mailgunConfig);
            var emailDefaultNotification = new EmailDefaultNotification { EmailAddresses = new List<string> { "a@b.com", "b@a.com" } };
            session.Expect(s => s.Load<EmailDefaultNotification>("EmailDefaultConfig")).Return(emailDefaultNotification);

            var message = new MailMessage();
            mailActioner.Expect(m => m.Send(Arg<MailgunConfiguration>.Is.Equal(mailgunConfig), Arg<MailMessage>.Is.NotNull)).WhenCalled(a => message = (MailMessage)(a.Arguments[1]));
            dateTimeMapper.Expect(d => d.DateTimeUtcToLocalWithOlsenZone(Arg<DateTime>.Is.Anything, Arg<string>.Is.Anything)).Return(DateTime.Now).Repeat.Any();

            var emailService = new EmailService { MailActioner = mailActioner, RavenDocStore = ravenDocStore, DateTimeOlsenFromUtcMapping = dateTimeMapper };
            var coordinatorComplete = new CoordinatorCompleteEmailWithSummary
                                          {
                                              CoordinatorId = Guid.NewGuid(),
                                              EmailAddresses = new List<string>(),
                                              FinishTimeUtc = DateTime.Now,
                                              StartTimeUtc = DateTime.Now.AddMinutes(-10),
                                              FailedCount = 4,
                                              SuccessCount = 3,
                                              Cost = 33.44m
                                          };
            emailService.Handle(coordinatorComplete);

            mailActioner.VerifyAllExpectations();
            Assert.That(message.From.ToString(), Is.EqualTo(mailgunConfig.DefaultFrom));
            Assert.That(message.To[0].Address, Is.EqualTo(emailDefaultNotification.EmailAddresses[0]));
            Assert.That(message.To[1].Address, Is.EqualTo(emailDefaultNotification.EmailAddresses[1]));
        }
    }
}
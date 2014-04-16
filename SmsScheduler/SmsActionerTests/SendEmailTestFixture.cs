using System;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsActioner;
using SmsActioner.InternalMessages.Commands;
using SmsActioner.InternalMessages.Responses;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Responses;

namespace SmsActionerTests
{
    [TestFixture]
    public class SendEmailTestFixture
    {
        // TODO : Add some kind of tracking / sendlocal
        [Test]
        public void SendEmailSaga_IsAccepted_WaitsForMoreInformation()
        {
            var sendOneEmailNow = new SendOneEmailNow();
            var emailSent = new EmailSent();
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(sagaData.EmailId)).Return(EmailStatus.Accepted);

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s => { 
                    s.Data = sagaData;
                    s.MailGun = mailgun;
                })
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .WhenSagaTimesOut();
        }

        [Test]
        public void SendEmailSaga_IsDelivered_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow();
            var emailSent = new EmailSent();
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(sagaData.EmailId)).Return(EmailStatus.Delivered);

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s => { 
                    s.Data = sagaData;
                    s.MailGun = mailgun;
                })
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent))
                    .ExpectReplyToOrginator<EmailDelivered>()
                .WhenSagaTimesOut();
        }

        [Test]
        public void SendEmailSaga_FailedDelivery_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow();
            var emailSent = new EmailSent();
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(sagaData.EmailId)).Return(EmailStatus.Failed);

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s => { 
                    s.Data = sagaData;
                    s.MailGun = mailgun;
                })
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent))
                    .ExpectReplyToOrginator<EmailDeliveryFailed>()
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_OpenedEmail_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow();
            var emailSent = new EmailSent();
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(sagaData.EmailId)).Return(EmailStatus.Opened);

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s => { 
                    s.Data = sagaData;
                    s.MailGun = mailgun;
                })
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent))
                    .ExpectReplyToOrginator<EmailDeliveredAndOpened>()
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_ClickedEmail_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow();
            var emailSent = new EmailSent();
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(sagaData.EmailId)).Return(EmailStatus.Clicked);

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s => { 
                    s.Data = sagaData;
                    s.MailGun = mailgun;
                })
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent))
                    .ExpectReplyToOrginator<EmailDeliveredAndClicked>()
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_UnsubscribeEmail_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow();
            var emailSent = new EmailSent();
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(sagaData.EmailId)).Return(EmailStatus.Unsubscribed);

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s => { 
                    s.Data = sagaData;
                    s.MailGun = mailgun;
                })
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent))
                    .ExpectReplyToOrginator<EmailUnsubscribed>()
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_Complained_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow();
            var emailSent = new EmailSent();
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(sagaData.EmailId)).Return(EmailStatus.Complained);

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s => { 
                    s.Data = sagaData;
                    s.MailGun = mailgun;
                })
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent))
                    .ExpectReplyToOrginator<EmailComplained>()
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }
    }
}
using System;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsActioner;
using SmsActioner.InternalMessages.Commands;
using SmsActioner.InternalMessages.Responses;
using SmsMessages;
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
        public void SendEmailSaga_IsDelivered_RepliesToSenderFirstTime_AndWaitsForMoreInformation()
        {
            var sendOneEmailNow = new SendOneEmailNow { CorrelationId = Guid.NewGuid() };
            var emailSent = new EmailSent { EmailId = "emailid" };
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Delivered);
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Delivered);

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
                    .ExpectReplyToOrginator<EmailDelivered>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                    .ExpectSendLocal<EmailDelivered>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((message, timespan) => timespan.Ticks == TimeSpan.FromHours(2).Ticks)
                .WhenSagaTimesOut()
                    .ExpectNotSendLocal<EmailDelivered>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((message, timespan) => timespan.Ticks == TimeSpan.FromHours(2).Ticks)
                .WhenSagaTimesOut(); 
        }

        [Test]
        public void SendEmailSaga_IsDelivered_DeliveredCountTimeoutIsTen_EndsSaga()
        {
            var sendOneEmailNow = new SendOneEmailNow { CorrelationId = Guid.NewGuid() };
            var emailSent = new EmailSent { EmailId = "emailid" };
            var sagaData = new EmailActionerData { Id = Guid.NewGuid(), DeliveredEmailCount = 11 };
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Delivered);
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Delivered);

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s =>
                {
                    s.Data = sagaData;
                    s.MailGun = mailgun;
                })
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent))
                    .ExpectNotSendLocal<EmailDelivered>(message => true)
                    .ExpectNoTimeoutToBeSetIn<EmailStatusPendingTimeout>()
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_FailedDelivery_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow { CorrelationId = Guid.NewGuid()};
            var emailSent = new EmailSent { EmailId = "emailid"};
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Failed);

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
                    .ExpectReplyToOrginator<EmailDeliveryFailed>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                    .ExpectSendLocal<EmailDeliveryFailed>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_OpenedEmail_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow { CorrelationId = Guid.NewGuid() };
            var emailSent = new EmailSent { EmailId = "emailid" };
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Opened);

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
                    .ExpectReplyToOrginator<EmailDeliveredAndOpened>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                    .ExpectSendLocal<EmailDeliveredAndOpened>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_ClickedEmail_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow { CorrelationId = Guid.NewGuid() };
            var emailSent = new EmailSent { EmailId = "emailid" };
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Clicked);

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
                    .ExpectReplyToOrginator<EmailDeliveredAndClicked>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                    .ExpectSendLocal<EmailDeliveredAndClicked>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_UnsubscribeEmail_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow { CorrelationId = Guid.NewGuid() };
            var emailSent = new EmailSent { EmailId = "emailid"};
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Unsubscribed);

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
                    .ExpectReplyToOrginator<EmailUnsubscribed>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                    .ExpectSendLocal<EmailUnsubscribed>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void SendEmailSaga_Complained_RepliesToSender()
        {
            var sendOneEmailNow = new SendOneEmailNow { CorrelationId = Guid.NewGuid() };
            var emailSent = new EmailSent { EmailId = "email id from service provider" };
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};
            var mailgun = MockRepository.GenerateMock<IMailGunWrapper>();
            mailgun.Expect(m => m.CheckStatus(emailSent.EmailId)).Return(EmailStatus.Complained);

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
                    .ExpectReplyToOrginator<EmailComplained>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                    .ExpectSendLocal<EmailComplained>(message => message.EmailId == emailSent.EmailId && message.CorrelationId == sendOneEmailNow.CorrelationId)
                .WhenSagaTimesOut()
                .AssertSagaCompletionIs(true);
        }
    }

    [TestFixture]
    public class SendEmailStatusCheckTestFixture
    {
        //[Test]
        //public void sendEmail()
        //{
        //    var mailGunWrapper = new MailGunWrapper() {DocumentStore = new RavenDocStore()};
        //    var message = new SendEmail
        //        {
        //            BaseRequest = new SendOneEmailNow
        //                {
        //                    BodyHtml = "<html><p>stuff</p></html>",
        //                    ToAddress = "samjeffress@gmail.com",
        //                    FromAddress = "samjeffress@gmail.com",
        //                    Subject = "subject"
        //                }
        //        };

        //    var email = mailGunWrapper.SendEmail(message);
        //}

        //[Test]
        //public void getEmail()
        //{
        //    var mailGunWrapper = new MailGunWrapper() {DocumentStore = new RavenDocStore()};
        //    var email = mailGunWrapper.CheckStatus("20140417110021.15689.25183@tricklesms.com");
        //}
    }
}
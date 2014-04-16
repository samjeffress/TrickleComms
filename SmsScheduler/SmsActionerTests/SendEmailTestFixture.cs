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
        [Test]
        public void SendEmailSaga_IsSent_RepliesToSender()
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
                    .ExpectReplyToOrginator<EmailSuccessfullyDelivered>()
                .WhenSagaTimesOut();
        }
    }
}
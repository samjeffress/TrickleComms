using System;
using NServiceBus.Testing;
using NUnit.Framework;
using SmsActioner;
using SmsActioner.InternalMessages.Commands;
using SmsActioner.InternalMessages.Responses;
using SmsMessages.MessageSending.Commands;

namespace SmsActionerTests
{
    [TestFixture]
    public class SendEmailTestFixture
    {
        [Test]
        public void SendEmailSaga()
        {
            var sendOneEmailNow = new SendOneEmailNow();
            var emailSent = new EmailSent();
            var sagaData = new EmailActionerData { Id = Guid.NewGuid()};

            Test.Initialize();
            Test.Saga<EmailActioner>()
                .WithExternalDependencies(s => s.Data = sagaData)
                .WhenReceivesMessageFrom("client")
                    .ExpectSendLocal<SendEmail>()
                .When(s => s.Handle(sendOneEmailNow))
                    .ExpectTimeoutToBeSetIn<EmailStatusPendingTimeout>((timeout, timespan) => timespan.Ticks == TimeSpan.FromMinutes(20).Ticks)
                .When(s => s.Handle(emailSent));
        }
    }
}
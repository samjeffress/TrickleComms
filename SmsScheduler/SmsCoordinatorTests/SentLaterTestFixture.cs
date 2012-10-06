using System;
using NServiceBus.Testing;
using NUnit.Framework;
using SmsCoordinator;
using SmsMessages;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class SentLaterTestFixture
    {
        [Test]
        public void ScheduleSmsForSendingLater()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAt = DateTime.Now.AddDays(1) };
            var sagaId = Guid.NewGuid();
            var messageSent = new MessageSent { CorrelationId = sagaId };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => a.Data = new ScheduledSmsData { Id = sagaId, Originator = "place", OriginalMessageId = "one" })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAt)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SendOneMessageNow>()
                .WhenSagaTimesOut()
                .When(s => s.Handle(messageSent))
                    .AssertSagaCompletionIs(true);
        }
    }
}
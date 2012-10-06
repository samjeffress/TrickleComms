using System;
using System.Collections.Generic;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
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

        [Test]
        public void TrickleThreeMessagesOverTenMinutes()
        {
            var startTime = DateTime.Now.AddHours(3);
            var duration = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsOverTimePeriod
            {
                StartTime = startTime,
                Messages = new List<SmsData>
                {
                    new SmsData("mobile#1", "message"), 
                    new SmsData("mobile#2", "message2"),
                    new SmsData("mobile#3", "message3")
                },
                Duration = duration
            };

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            var messageTiming = new List<DateTime> {startTime, startTime.AddMinutes(5), startTime.AddMinutes(10)};
            timingManager.Expect(t => t.CalculateTiming(startTime, duration, 3))
                .Return(messageTiming);

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s => s.TimingManager = timingManager)
                    .ExpectSend<ScheduleSmsForSendingLater>(a => a.SendMessageAt == messageTiming[0])
                    .ExpectSend<ScheduleSmsForSendingLater>(a => a.SendMessageAt == messageTiming[1])
                    .ExpectSend<ScheduleSmsForSendingLater>(a => a.SendMessageAt == messageTiming[2])
                .When(s => s.Handle(trickleMultipleMessages))
                .AssertSagaCompletionIs(false);

            timingManager.VerifyAllExpectations();
        }
    }
}
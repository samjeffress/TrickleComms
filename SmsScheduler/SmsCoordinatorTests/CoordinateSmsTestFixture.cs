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
    public class CoordinateSmsTestFixture
    {
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
            var messageTiming = new List<DateTime> { startTime, startTime.AddMinutes(5), startTime.AddMinutes(10) };
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

        [Test]
        public void TrickleThreeMessagesSpacedAMinuteApart()
        {
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsSpacedByTimePeriod
            {
                StartTime = startTime,
                Messages = new List<SmsData>
                {
                    new SmsData("mobile#1", "message"), 
                    new SmsData("mobile#2", "message2"),
                    new SmsData("mobile#3", "message3")
                },
                TimeSpacing = timeSpacing
            };

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                    .ExpectSend<ScheduleSmsForSendingLater>(a => a.SendMessageAt == startTime)
                    .ExpectSend<ScheduleSmsForSendingLater>(a => a.SendMessageAt == startTime + timeSpacing)
                    .ExpectSend<ScheduleSmsForSendingLater>(a => a.SendMessageAt == startTime + timeSpacing + timeSpacing)
                .When(s => s.Handle(trickleMultipleMessages))
                .AssertSagaCompletionIs(false);
        }
    }
}
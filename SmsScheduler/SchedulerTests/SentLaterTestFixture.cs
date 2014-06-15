using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Responses;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;
using SmsMessages.Tracking.Scheduling.Commands;
using SmsScheduler;

namespace SmsSchedulerTests
{
    [TestFixture]
    public class SentLaterTestFixture
    {
        [Test]
        public void ScheduleSmsForSendingLater()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), ScheduleMessageId = Guid.NewGuid() };
            var sagaId = Guid.NewGuid();
            var messageSent = new MessageSuccessfullyDelivered { ConfirmationData = new SmsConfirmationData("a", DateTime.Now, 3), SmsData = new SmsData("1", "2") };
         
            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc)
                    .ExpectSendLocal<ScheduleCreated>()
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SendOneMessageNow>()
                .WhenSagaTimesOut()
                    .ExpectPublish<ScheduledSmsSent>()
                    .ExpectSendLocal<ScheduleSucceeded>(s => { return s.ConfirmationData == messageSent.ConfirmationData && s.ScheduleId == scheduleSmsForSendingLater.ScheduleMessageId;})
                .When(s => s.Handle(messageSent))
                    .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleSmsForSendingLaterButFails()
        {

            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), ScheduleMessageId = Guid.NewGuid()};
            var sagaId = Guid.NewGuid();
            var messageFailed = new MessageFailedSending { SmsData = new SmsData("1", "2"), SmsFailed = new SmsFailed(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) };

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SendOneMessageNow>()
                .WhenSagaTimesOut()
                    .ExpectPublish<ScheduledSmsFailed>()
                    .ExpectSendLocal<ScheduleFailed>()
                .When(s => s.Handle(messageFailed))
                    .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPaused()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), SmsData = new SmsData("1", "2"), ScheduleMessageId = Guid.NewGuid()};
            var sagaId = Guid.NewGuid();

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = "one",
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc)
                    .ExpectSendLocal<ScheduleCreated>()
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectPublish<MessageSchedulePaused>()
                    .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Paused)
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                    .ExpectNotSend<SendOneMessageNow>(now => false)
                .WhenSagaTimesOut();
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPausedThenResumedAndSent()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), SmsData = new SmsData("1", "2") };
            var sagaId = Guid.NewGuid();

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                    .ExpectSendLocal<ScheduleCreated>()
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectPublish<MessageSchedulePaused>()
                    .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Paused)
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => state.TimeoutCounter == 1)
                    .ExpectPublish<MessageRescheduled>()
                    .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Scheduled)
                .When(s => s.Handle(new ResumeScheduledMessageWithOffset(Guid.Empty, new TimeSpan())))
                    .ExpectNotSend<SendOneMessageNow>(now => false)
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 0 }))
                    .ExpectSend<SendOneMessageNow>()
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 1 }))
                    .ExpectPublish<ScheduledSmsSent>()
                    .ExpectSendLocal<ScheduleSucceeded>()
                .When(s => s.Handle(new MessageSuccessfullyDelivered { ConfirmationData = new SmsConfirmationData("a", DateTime.Now, 3), SmsData = new SmsData("1", "2") }))
                    .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPausedThenRescheduledAndSent()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), SmsData = new SmsData("1", "2"), ScheduleMessageId = Guid.NewGuid()};
            var sagaId = Guid.NewGuid();

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                    .ExpectSendLocal<ScheduleCreated>()
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectPublish<MessageSchedulePaused>()
                    .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Paused)
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => state.TimeoutCounter == 1)
                    .ExpectPublish<MessageRescheduled>()
                    .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Scheduled)
                .When(s => s.Handle(new RescheduleScheduledMessageWithNewTime(Guid.Empty, new DateTime(2040,4,4,4,4,4, DateTimeKind.Utc))))
                    .ExpectNotSend<SendOneMessageNow>(now => false)
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 0 }))
                    .ExpectSend<SendOneMessageNow>()
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 1 }))
                    .ExpectPublish<ScheduledSmsSent>()
                    .ExpectSendLocal<ScheduleSucceeded>()
                .When(s => s.Handle(new MessageSuccessfullyDelivered { ConfirmationData = new SmsConfirmationData("a", DateTime.Now, 3), SmsData = new SmsData("1", "2") }))
                    .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPausedThenResumedOutOfOrderAndSent()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), SmsData = new SmsData("1", "2"), ScheduleMessageId = Guid.NewGuid()};
            var sagaId = Guid.NewGuid();

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater
                    {
                        SmsData = new SmsData("1", "msg"), 
                        SmsMetaData = new SmsMetaData()
                    }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                    .ExpectSendLocal<ScheduleCreated>()
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => state.TimeoutCounter == 1)
                    .ExpectPublish<MessageRescheduled>()
                    .ExpectSendLocal<ScheduleStatusChanged>(s => s.Status == MessageStatus.Scheduled)
                .When(s => s.Handle(new ResumeScheduledMessageWithOffset(Guid.Empty, new TimeSpan()) { MessageRequestTimeUtc = DateTime.Now }))
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty) { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-10)}))
                    .ExpectSend<SendOneMessageNow>()
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 1}))
                    .ExpectPublish<ScheduledSmsSent>()
                    .ExpectSendLocal<ScheduleSucceeded>()
                .When(s => s.Handle(new MessageSuccessfullyDelivered { ConfirmationData = new SmsConfirmationData("a", DateTime.Now, 3), SmsData = new SmsData("1", "2") }))
                    .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ResumePausedSchedule_Data()
        {
            var sagaId = Guid.NewGuid();
            var scheduleMessageId = Guid.NewGuid();

            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater {SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData(), SendMessageAtUtc = DateTime.Now};
            var scheduledSmsData = new ScheduledSmsData
            {
                Id = sagaId,
                ScheduleMessageId = scheduleMessageId,
                Originator = "place",
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = scheduleSmsForSendingLater,
                OriginalMessageData = new OriginalMessageData(scheduleSmsForSendingLater)
            };

            Test.Initialize();
            var rescheduleMessage = new ResumeScheduledMessageWithOffset(scheduleMessageId, new TimeSpan(0, 1, 0, 0));
            var rescheduledTime = scheduledSmsData.OriginalMessage.SendMessageAtUtc.Add(rescheduleMessage.Offset);
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, span) => span == rescheduledTime)
                    .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(rescheduleMessage));
        }

        [Test]
        public void ReschedulePausedSchedule_Data()
        {
            var sagaId = Guid.NewGuid();
            var scheduleMessageId = Guid.NewGuid();

            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater {SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData(), SendMessageAtUtc = DateTime.Now};
            var scheduledSmsData = new ScheduledSmsData
            {
                Id = sagaId,
                ScheduleMessageId = scheduleMessageId,
                Originator = "place",
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = scheduleSmsForSendingLater,
                OriginalMessageData = new OriginalMessageData(scheduleSmsForSendingLater)
            };

            Test.Initialize();
            var rescheduleMessage = new RescheduleScheduledMessageWithNewTime(scheduleMessageId, new DateTime(2040, 4, 4, 4,4,4, DateTimeKind.Utc));
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, span) => span == rescheduleMessage.NewScheduleTimeUtc)
                    .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(rescheduleMessage));

            Assert.IsFalse(scheduledSmsData.SchedulingPaused);
        }

        [Test]
        public void TimeoutPromptsMessageSending_Data()
        {
            var bus = MockRepository.GenerateMock<IBus>();

            var sendOneMessageNow = new SendOneMessageNow();
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.NotNull))
                .WhenCalled(i => sendOneMessageNow = (SendOneMessageNow)((i.Arguments[0])));

            var dataId = Guid.NewGuid();
            var originalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("3443", "message"), SmsMetaData = new SmsMetaData { Tags = new List<string> { "a", "b" }, Topic = "topic" } };
            var data = new ScheduledSmsData { Id = dataId, OriginalMessage = originalMessage};

            var scheduleSms = new ScheduleSms { Bus = bus, Data = data };
            var timeoutMessage = new ScheduleSmsTimeout();
            scheduleSms.Timeout(timeoutMessage);

            Assert.That(sendOneMessageNow.SmsData, Is.EqualTo(data.OriginalMessage.SmsData));
            Assert.That(sendOneMessageNow.SmsMetaData, Is.EqualTo(data.OriginalMessage.SmsMetaData));
            Assert.That(sendOneMessageNow.CorrelationId, Is.EqualTo(data.Id));

            bus.VerifyAllExpectations();
        }

        [Test]
        public void TimeoutSendingPausedNoAction_Data()
        {
            var bus = MockRepository.GenerateStrictMock<IBus>();

            var dataId = Guid.NewGuid();
            var originalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("3443", "message"), SmsMetaData = new SmsMetaData { Tags = new List<string> { "a", "b" }, Topic = "topic" } };
            var data = new ScheduledSmsData { Id = dataId, OriginalMessage = originalMessage, SchedulingPaused = true };

            var scheduleSms = new ScheduleSms { Bus = bus, Data = data };
            var timeoutMessage = new ScheduleSmsTimeout();
            scheduleSms.Timeout(timeoutMessage);

            bus.VerifyAllExpectations();
        }

        [Test]
        public void OriginalMessageGetsSavedToSaga_Data()
        {
            var data = new ScheduledSmsData();
            var originalMessage = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now };
            
            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("address")
                    .ExpectPublish<SmsScheduled>(m => m.CoordinatorId == data.Id && m.ScheduleMessageId == originalMessage.ScheduleMessageId)
                .When(s => s.Handle(originalMessage));

            Assert.That(data.OriginalMessage, Is.EqualTo(originalMessage));
        }

        [Test]
        public void PauseMessageSetsSchedulePauseFlag_Data()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater {SmsData = new SmsData("1", "2")};
            var data = new ScheduledSmsData
                {
                    OriginalMessage = scheduleSmsForSendingLater,
                    OriginalMessageData = new OriginalMessageData(scheduleSmsForSendingLater)
                };
            var scheduleId = Guid.NewGuid();
            var pauseScheduledMessageIndefinitely = new PauseScheduledMessageIndefinitely(scheduleId);
            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = data; })
                .WhenReceivesMessageFrom("place")
                    .ExpectSendLocal<ScheduleStatusChanged>(s =>
                        {
                            return s.Status == MessageStatus.Paused &&
                            s.ScheduleId == pauseScheduledMessageIndefinitely.ScheduleMessageId &&
                            s.RequestTimeUtc == pauseScheduledMessageIndefinitely.MessageRequestTimeUtc;
                        })

                .When(s => s.Handle(pauseScheduledMessageIndefinitely));

            Assert.IsTrue(data.SchedulingPaused);
        }
    }
}

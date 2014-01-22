using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using SmsMessages.MessageSending.Events;
using SmsMessages.MessageSending.Responses;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;
using SmsScheduler;
using SmsTrackingModels;

namespace SmsSchedulerTests
{
    [TestFixture]
    public class SentLaterTestFixture
    {
        public IDocumentStore DocumentStore { get; set; }

        public SentLaterTestFixture()
        {
            DocumentStore = new EmbeddableDocumentStore { RunInMemory = true };
            DocumentStore.Initialize();
        }

        [Test]
        public void ScheduleSmsForSendingLater()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), ScheduleMessageId = Guid.NewGuid() };
            var sagaId = Guid.NewGuid();
            var messageSent = new MessageSent { ConfirmationData = new SmsConfirmationData("a", DateTime.Now, 3), SmsData = new SmsData("1", "2") };
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());
            StoreDocument(new ScheduleTrackingData { ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId, MessageStatus = MessageStatus.WaitingForScheduling }, scheduleSmsForSendingLater.ScheduleMessageId.ToString());

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; a.RavenDocStore = ravenDocStore; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SendOneMessageNow>()
                .WhenSagaTimesOut()
                    .ExpectPublish<ScheduledSmsSent>()
                .When(s => s.Handle(messageSent))
                    .AssertSagaCompletionIs(true);

            var scheduleTrackingData = GetSchedule(scheduleSmsForSendingLater.ScheduleMessageId.ToString());
            Assert.That(scheduleTrackingData.MessageStatus, Is.EqualTo(MessageStatus.Sent));
        }

        [Test]
        public void ScheduleSmsForSendingLaterButFails()
        {

            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), ScheduleMessageId = Guid.NewGuid()};
            var sagaId = Guid.NewGuid();
            var messageFailed = new MessageFailedSending { SmsData = new SmsData("1", "2"), SmsFailed = new SmsFailed(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) };
            
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());
            StoreDocument(new ScheduleTrackingData { ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId, MessageStatus = MessageStatus.WaitingForScheduling }, scheduleSmsForSendingLater.ScheduleMessageId.ToString());

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; a.RavenDocStore = ravenDocStore; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SendOneMessageNow>()
                .WhenSagaTimesOut()
                    .ExpectPublish<ScheduledSmsFailed>()
                .When(s => s.Handle(messageFailed))
                    .AssertSagaCompletionIs(true);

            var scheduleTrackingData = GetSchedule(scheduleSmsForSendingLater.ScheduleMessageId.ToString());
            Assert.That(scheduleTrackingData.MessageStatus, Is.EqualTo(MessageStatus.Failed));
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPaused()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), SmsData = new SmsData("1", "2"), ScheduleMessageId = Guid.NewGuid()};
            var sagaId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());
            StoreDocument(new ScheduleTrackingData { ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId, MessageStatus = MessageStatus.WaitingForScheduling }, scheduleSmsForSendingLater.ScheduleMessageId.ToString());

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = "one",
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; a.RavenDocStore = ravenDocStore; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectPublish<MessageSchedulePaused>()
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                    .ExpectNotSend<SendOneMessageNow>(now => false)
                .WhenSagaTimesOut();

            var scheduleTrackingData = GetSchedule(scheduleSmsForSendingLater.ScheduleMessageId.ToString());
            Assert.That(scheduleTrackingData.MessageStatus, Is.EqualTo(MessageStatus.Paused));
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPausedThenResumedAndSent()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), SmsData = new SmsData("1", "2") };
            var sagaId = Guid.NewGuid();
            
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());
            StoreDocument(new ScheduleTrackingData { ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId, MessageStatus = MessageStatus.WaitingForScheduling }, scheduleSmsForSendingLater.ScheduleMessageId.ToString());

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; a.RavenDocStore = ravenDocStore; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectPublish<MessageSchedulePaused>()
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => state.TimeoutCounter == 1)
                    .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(new ResumeScheduledMessageWithOffset(Guid.Empty, new TimeSpan())))
                    .ExpectNotSend<SendOneMessageNow>(now => false)
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 0 }))
                    .ExpectSend<SendOneMessageNow>()
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 1 }))
                    .ExpectPublish<ScheduledSmsSent>()
                .When(s => s.Handle(new MessageSent { ConfirmationData = new SmsConfirmationData("a", DateTime.Now, 3), SmsData = new SmsData("1", "2")}))
                    .AssertSagaCompletionIs(true);
            var scheduleTrackingData = GetSchedule(scheduleSmsForSendingLater.ScheduleMessageId.ToString());
            Assert.That(scheduleTrackingData.MessageStatus, Is.EqualTo(MessageStatus.Sent));
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPausedThenRescheduledAndSent()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), SmsData = new SmsData("1", "2"), ScheduleMessageId = Guid.NewGuid()};
            var sagaId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());
            StoreDocument(new ScheduleTrackingData { ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId, MessageStatus = MessageStatus.WaitingForScheduling }, scheduleSmsForSendingLater.ScheduleMessageId.ToString());
            
            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; a.RavenDocStore = ravenDocStore; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectPublish<MessageSchedulePaused>()
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => state.TimeoutCounter == 1)
                    .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(new RescheduleScheduledMessageWithNewTime(Guid.Empty, new DateTime(2040,4,4,4,4,4, DateTimeKind.Utc))))
                    .ExpectNotSend<SendOneMessageNow>(now => false)
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 0 }))
                    .ExpectSend<SendOneMessageNow>()
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 1 }))
                    .ExpectPublish<ScheduledSmsSent>()
                .When(s => s.Handle(new MessageSent { ConfirmationData = new SmsConfirmationData("a", DateTime.Now, 3), SmsData = new SmsData("1", "2")}))
                    .AssertSagaCompletionIs(true);

            var scheduleTrackingData = GetSchedule(scheduleSmsForSendingLater.ScheduleMessageId.ToString());
            Assert.That(scheduleTrackingData.MessageStatus, Is.EqualTo(MessageStatus.Sent));
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPausedThenResumedOutOfOrderAndSent()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAtUtc = DateTime.Now.AddDays(1), SmsData = new SmsData("1", "2"), ScheduleMessageId = Guid.NewGuid()};
            var sagaId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());
            StoreDocument(new ScheduleTrackingData { ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId, MessageStatus = MessageStatus.WaitingForScheduling }, scheduleSmsForSendingLater.ScheduleMessageId.ToString());
            
            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; a.RavenDocStore = ravenDocStore; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAtUtc && state.TimeoutCounter == 0)
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => state.TimeoutCounter == 1)
                    .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(new ResumeScheduledMessageWithOffset(Guid.Empty, new TimeSpan()) { MessageRequestTimeUtc = DateTime.Now }))
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty) { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-10)}))
                    .ExpectSend<SendOneMessageNow>()
                .When(s => s.Timeout(new ScheduleSmsTimeout { TimeoutCounter = 1}))
                    .ExpectPublish<ScheduledSmsSent>()
                .When(s => s.Handle(new MessageSent { ConfirmationData = new SmsConfirmationData("a", DateTime.Now, 3), SmsData = new SmsData("1", "2") }))
                    .AssertSagaCompletionIs(true);

            var scheduleTrackingData = GetSchedule(scheduleSmsForSendingLater.ScheduleMessageId.ToString());
            Assert.That(scheduleTrackingData.MessageStatus, Is.EqualTo(MessageStatus.Sent));
        }

        [Test]
        public void ResumePausedSchedule_Data()
        {
            var sagaId = Guid.NewGuid();
            var scheduleMessageId = Guid.NewGuid();
            StoreDocument(new ScheduleTrackingData { ScheduleId = scheduleMessageId, MessageStatus = MessageStatus.Paused }, scheduleMessageId.ToString());
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());

            var scheduledSmsData = new ScheduledSmsData
            {
                Id = sagaId,
                ScheduleMessageId = scheduleMessageId,
                Originator = "place",
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData(),SendMessageAtUtc = DateTime.Now }
            };

            Test.Initialize();
            var rescheduleMessage = new ResumeScheduledMessageWithOffset(scheduleMessageId, new TimeSpan(0, 1, 0, 0));
            var rescheduledTime = scheduledSmsData.OriginalMessage.SendMessageAtUtc.Add(rescheduleMessage.Offset);
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; a.RavenDocStore = ravenDocStore; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, span) => span == rescheduledTime)
                    .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(rescheduleMessage));

            var schedule = GetSchedule(scheduleMessageId.ToString());
            Assert.That(schedule.MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(schedule.ScheduleTimeUtc, Is.EqualTo(rescheduledTime));
        }

        [Test]
        public void ReschedulePausedSchedule_Data()
        {
            var sagaId = Guid.NewGuid();
            var scheduleMessageId = Guid.NewGuid(); 
            StoreDocument(new ScheduleTrackingData { ScheduleId = scheduleMessageId, MessageStatus = MessageStatus.Paused }, scheduleMessageId.ToString());
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());

            var scheduledSmsData = new ScheduledSmsData
            {
                Id = sagaId,
                ScheduleMessageId = scheduleMessageId,
                Originator = "place",
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData(),SendMessageAtUtc = DateTime.Now }
            };

            Test.Initialize();
            var rescheduleMessage = new RescheduleScheduledMessageWithNewTime(scheduleMessageId, new DateTime(2040, 4, 4, 4,4,4, DateTimeKind.Utc));
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = scheduledSmsData; a.RavenDocStore = ravenDocStore; })
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, span) => span == rescheduleMessage.NewScheduleTimeUtc)
                    .ExpectPublish<MessageRescheduled>()
                .When(s => s.Handle(rescheduleMessage));

            var schedule = GetSchedule(scheduleMessageId.ToString());
            Assert.That(schedule.MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(schedule.ScheduleTimeUtc, Is.EqualTo(rescheduleMessage.NewScheduleTimeUtc));
        }

        [Test]
        public void TimeoutPromptsMessageSending_Data()
        {
            var bus = MockRepository.GenerateMock<IBus>();

            var sendOneMessageNow = new SendOneMessageNow();
            bus.Expect(b => b.Send(Arg<SendOneMessageNow>.Is.NotNull))
                .WhenCalled(i => sendOneMessageNow = (SendOneMessageNow)((object[])(i.Arguments[0]))[0]);

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
            StoreDocument(new ScheduleTrackingData { ScheduleId = originalMessage.ScheduleMessageId, MessageStatus = MessageStatus.WaitingForScheduling}, originalMessage.ScheduleMessageId.ToString());
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = data; a.RavenDocStore = ravenDocStore; })
                .WhenReceivesMessageFrom("address")
                    .ExpectPublish<SmsScheduled>(m => m.CoordinatorId == data.Id && m.ScheduleMessageId == originalMessage.ScheduleMessageId)
                .When(s => s.Handle(originalMessage));

            Assert.That(data.OriginalMessage, Is.EqualTo(originalMessage));
            var schedule = GetSchedule(originalMessage.ScheduleMessageId.ToString());
            Assert.That(schedule.MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
        }

        [Test]
        public void PauseMessageSetsSchedulePauseFlag_Data()
        {
            var data = new ScheduledSmsData { OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "2")}};
            var scheduleId = Guid.NewGuid();
            var pauseScheduledMessageIndefinitely = new PauseScheduledMessageIndefinitely(scheduleId);
            StoreDocument(new ScheduleTrackingData { ScheduleId = data.OriginalMessage.ScheduleMessageId, MessageStatus = MessageStatus.Scheduled}, data.OriginalMessage.ScheduleMessageId.ToString());
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore().OpenSession("SmsTracking")).Return(DocumentStore.OpenSession());

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => { a.Data = data; a.RavenDocStore = ravenDocStore; })
                .WhenReceivesMessageFrom("place")
                .When(s => s.Handle(pauseScheduledMessageIndefinitely));

            Assert.IsTrue(data.SchedulingPaused);
            var schedule = GetSchedule(data.OriginalMessage.ScheduleMessageId.ToString());
            Assert.That(schedule.MessageStatus, Is.EqualTo(MessageStatus.Paused));
        }

        private void StoreDocument(object obj, string id)
        {
            using (var session = DocumentStore.OpenSession())
            {
                session.Store(obj, id);
                session.SaveChanges();
            }
        }

        private ScheduleTrackingData GetSchedule(string id)
        {
            using (var session = DocumentStore.OpenSession())
            {
                return session.Load<ScheduleTrackingData>(id);
            }
        }
    }
}
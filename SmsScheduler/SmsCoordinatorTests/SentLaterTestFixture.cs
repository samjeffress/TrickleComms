using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using SmsMessages.Scheduling;
using SmsMessages.Tracking;

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

            var scheduledSmsData = new ScheduledSmsData 
            {
                Id = sagaId, 
                Originator = "place", 
                OriginalMessageId = Guid.NewGuid().ToString(),
                OriginalMessage = new ScheduleSmsForSendingLater { SmsData = new SmsData("1", "msg"), SmsMetaData = new SmsMetaData() }
            };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => a.Data = scheduledSmsData)
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAt)
                    .ExpectSend<ScheduleCreated>()
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SendOneMessageNow>()
                .WhenSagaTimesOut()
                    .ExpectReplyToOrginator<ScheduledSmsSent>()
                    .ExpectSend<ScheduleComplete>()
                .When(s => s.Handle(messageSent))
                    .AssertSagaCompletionIs(true);
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPaused()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAt = DateTime.Now.AddDays(1) };
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
                .WithExternalDependencies(a => a.Data = scheduledSmsData)
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAt)
                    .ExpectSend<ScheduleCreated>()
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SchedulePaused>()
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                    .ExpectNotSend<SendOneMessageNow>(now => false)
                .WhenSagaTimesOut();
        }

        [Test]
        public void ScheduleSmsForSendingLaterButIsPausedThenResumedAndSent()
        {
            var scheduleSmsForSendingLater = new ScheduleSmsForSendingLater { SendMessageAt = DateTime.Now.AddDays(1) };
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
                .WithExternalDependencies(a => a.Data = scheduledSmsData)
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>((state, timeout) => timeout == scheduleSmsForSendingLater.SendMessageAt)
                    .ExpectSend<ScheduleCreated>()
                .When(s => s.Handle(scheduleSmsForSendingLater))
                    .ExpectSend<SchedulePaused>()
                .When(s => s.Handle(new PauseScheduledMessageIndefinitely(Guid.Empty)))
                    .ExpectNotSend<SendOneMessageNow>(now => false)
                .WhenSagaTimesOut()
                    .ExpectTimeoutToBeSetAt<ScheduleSmsTimeout>()
                    .ExpectSend<ScheduleResumed>()
                .When(s => s.Handle(new ResumeScheduledMessageWithOffset(Guid.Empty, new TimeSpan())))
                    .ExpectSend<SendOneMessageNow>()
                .WhenSagaTimesOut()
                    .ExpectReplyToOrginator<ScheduledSmsSent>()
                    .ExpectSend<ScheduleComplete>()
                .When(s => s.Handle(new MessageSent()))
                    .AssertSagaCompletionIs(true);
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
            var originalMessage = new ScheduleSmsForSendingLater { SendMessageAt = DateTime.Now };

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => a.Data = data)
                .WhenReceivesMessageFrom("address")
                    .ExpectReplyToOrginator<SmsScheduled>(m => m.CoordinatorId == data.Id && m.ScheduleMessageId == originalMessage.ScheduleMessageId)
                    .ExpectSend<ScheduleCreated>(m => m.ScheduleId == originalMessage.ScheduleMessageId && m.SmsData == originalMessage.SmsData && m.SmsMetaData == originalMessage.SmsMetaData && m.CallerId == data.Id)
                .When(s => s.Handle(originalMessage));

            Assert.That(data.OriginalMessage, Is.EqualTo(originalMessage));
        }

        [Test]
        public void PauseMessageSetsSchedulePauseFlag_Data()
        {
            var data = new ScheduledSmsData();
            var scheduleId = Guid.NewGuid();
            var pauseScheduledMessageIndefinitely = new PauseScheduledMessageIndefinitely(scheduleId);

            Test.Initialize();
            Test.Saga<ScheduleSms>()
                .WithExternalDependencies(a => a.Data = data)
                .WhenReceivesMessageFrom("place")
                    .ExpectSend<SchedulePaused>(s => s.ScheduleId == scheduleId)
                .When(s => s.Handle(pauseScheduledMessageIndefinitely));

            Assert.IsTrue(data.SchedulingPaused);
        }
    }
}
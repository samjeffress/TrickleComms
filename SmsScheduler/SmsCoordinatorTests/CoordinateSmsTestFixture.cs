using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.Commands;
using SmsMessages.CommonData;

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

            var sagaData = new CoordinateSmsSchedulingData { Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                    {
                        s.TimingManager = timingManager;
                        s.Data = sagaData;
                    })
                    .ExpectSend<List<ScheduleSmsForSendingLater>>()
                .When(s => s.Handle(trickleMultipleMessages))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .AssertSagaCompletionIs(true);

            timingManager.VerifyAllExpectations();
            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSent, Is.EqualTo(3));
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

            var sagaData = new CoordinateSmsSchedulingData {Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(d => d.Data = sagaData)
                    .ExpectSend<List<ScheduleSmsForSendingLater>>()
                .When(s => s.Handle(trickleMultipleMessages))
                .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .AssertSagaCompletionIs(true);

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSent, Is.EqualTo(3));
        }

        [Test]
        public void TrickleThreeMessagesSpacedAMinuteApartPausedAfterFirstThenResumed()
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

            var sagaData = new CoordinateSmsSchedulingData { Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(d => d.Data = sagaData)
                    .ExpectSend<List<ScheduleSmsForSendingLater>>()
                .When(s => s.Handle(trickleMultipleMessages))
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .ExpectSend<List<PauseScheduledMessageIndefinitely>>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                    .ExpectSend<List<ResumeScheduledMessageWithOffset>>()
                .When(s => s.Handle(new ResumeTrickledMessages()))
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent()))
                    .AssertSagaCompletionIs(true);

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSent, Is.EqualTo(3));
        }

        [Test]
        public void TrickleMessagesOverPeriod_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj")};
            var trickleMessagesOverTime = new TrickleSmsOverTimePeriod { Duration = new TimeSpan(1000), Messages = messageList, StartTime = DateTime.Now };

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            var bus = MockRepository.GenerateMock<IBus>();
            
            var datetimeSpacing = new List<DateTime> { DateTime.Now.AddMinutes(10), DateTime.Now.AddMinutes(20) };
            timingManager.Expect(
                t =>
                t.CalculateTiming(trickleMessagesOverTime.StartTime, trickleMessagesOverTime.Duration, trickleMessagesOverTime.Messages.Count))
                .Return(datetimeSpacing);

            var scheduleSmsForLaterList = new List<ScheduleSmsForSendingLater>();
            bus.Expect(b => b.Send(Arg<ScheduleSmsForSendingLater>.Is.NotNull))
                .WhenCalled(i => scheduleSmsForLaterList = (List<ScheduleSmsForSendingLater>)((object[])(i.Arguments[0]))[0]);

            var sagaData = new CoordinateSmsSchedulingData();
            var smsScheduler = new CoordinateSmsScheduler { Bus = bus, TimingManager = timingManager,Data = sagaData};
            smsScheduler.Handle(trickleMessagesOverTime);

            Assert.That(scheduleSmsForLaterList[0].SendMessageAt, Is.EqualTo(datetimeSpacing[0]));
            Assert.That(scheduleSmsForLaterList[0].SmsData.Message, Is.EqualTo(trickleMessagesOverTime.Messages[0].Message));
            Assert.That(scheduleSmsForLaterList[0].SmsData.Mobile, Is.EqualTo(trickleMessagesOverTime.Messages[0].Mobile));
            Assert.That(scheduleSmsForLaterList[0].SmsMetaData, Is.EqualTo(trickleMessagesOverTime.MetaData));

            Assert.That(scheduleSmsForLaterList[1].SendMessageAt, Is.EqualTo(datetimeSpacing[1]));
            Assert.That(scheduleSmsForLaterList[1].SmsData.Message, Is.EqualTo(trickleMessagesOverTime.Messages[1].Message));
            Assert.That(scheduleSmsForLaterList[1].SmsData.Mobile, Is.EqualTo(trickleMessagesOverTime.Messages[1].Mobile));
            Assert.That(scheduleSmsForLaterList[1].SmsMetaData, Is.EqualTo(trickleMessagesOverTime.MetaData));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(sagaData.ScheduledMessageStatus[0].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[0]));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(sagaData.ScheduledMessageStatus[1].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[1]));
            timingManager.VerifyAllExpectations();
            bus.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesSpacedByTimespan_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj") };
            var trickleMessagesOverTime = new TrickleSmsSpacedByTimePeriod {  TimeSpacing = new TimeSpan(1000), Messages = messageList, StartTime = DateTime.Now };

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            var bus = MockRepository.GenerateMock<IBus>();

            var scheduleSmsForLaterList = new List<ScheduleSmsForSendingLater>();
            bus.Expect(b => b.Send(Arg<ScheduleSmsForSendingLater>.Is.NotNull))
                .WhenCalled(i => scheduleSmsForLaterList = (List<ScheduleSmsForSendingLater>)((object[])(i.Arguments[0]))[0]);

            var sagaData = new CoordinateSmsSchedulingData();
            var smsScheduler = new CoordinateSmsScheduler { Bus = bus, Data = sagaData };
            smsScheduler.Handle(trickleMessagesOverTime);

            Assert.That(scheduleSmsForLaterList[0].SendMessageAt.Ticks, Is.EqualTo(trickleMessagesOverTime.StartTime.Ticks));
            Assert.That(scheduleSmsForLaterList[0].SmsData.Message, Is.EqualTo(trickleMessagesOverTime.Messages[0].Message));
            Assert.That(scheduleSmsForLaterList[0].SmsData.Mobile, Is.EqualTo(trickleMessagesOverTime.Messages[0].Mobile));
            Assert.That(scheduleSmsForLaterList[0].SmsMetaData, Is.EqualTo(trickleMessagesOverTime.MetaData));

            Assert.That(scheduleSmsForLaterList[1].SendMessageAt.Ticks, Is.EqualTo(trickleMessagesOverTime.StartTime.Ticks + trickleMessagesOverTime.TimeSpacing.Ticks));
            Assert.That(scheduleSmsForLaterList[1].SmsData.Message, Is.EqualTo(trickleMessagesOverTime.Messages[1].Message));
            Assert.That(scheduleSmsForLaterList[1].SmsData.Mobile, Is.EqualTo(trickleMessagesOverTime.Messages[1].Mobile));
            Assert.That(scheduleSmsForLaterList[1].SmsMetaData, Is.EqualTo(trickleMessagesOverTime.MetaData));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(sagaData.ScheduledMessageStatus[0].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[0]));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(sagaData.ScheduledMessageStatus[1].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[1]));
            timingManager.VerifyAllExpectations();
            bus.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesPauseMessagesIndefinitely_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj"), new SmsData("mobile", "sent") };

            var pauseMessageSending = new PauseTrickledMessagesIndefinitely();

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            var bus = MockRepository.GenerateMock<IBus>();

            var pauseScheduledMessageIndefinitely = new List<PauseScheduledMessageIndefinitely>();
            bus.Expect(b => b.Send(Arg<PauseScheduledMessageIndefinitely>.Is.NotNull))
                .WhenCalled(i => pauseScheduledMessageIndefinitely = (List<PauseScheduledMessageIndefinitely>)((object[])(i.Arguments[0]))[0]);

            var scheduledMessageStatuses = new List<ScheduledMessageStatus> 
            {
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[0]}),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[1]}),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[2]}, MessageStatus.Sent)
            };

            var sagaData = new CoordinateSmsSchedulingData { ScheduledMessageStatus = scheduledMessageStatuses};
            var smsScheduler = new CoordinateSmsScheduler { Bus = bus, Data = sagaData };
            smsScheduler.Handle(pauseMessageSending);

            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.Paused));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.Paused));

            Assert.That(pauseScheduledMessageIndefinitely.Count, Is.EqualTo(2));
            Assert.That(pauseScheduledMessageIndefinitely[0].ScheduleMessageId, Is.EqualTo(scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId));
            Assert.That(pauseScheduledMessageIndefinitely[1].ScheduleMessageId, Is.EqualTo(scheduledMessageStatuses[1].ScheduledSms.ScheduleMessageId));

            timingManager.VerifyAllExpectations();
            bus.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesResumeMessageSending_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj"), new SmsData("mobile", "sent") };

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            var bus = MockRepository.GenerateMock<IBus>();

            var resumeScheduledMessages = new List<ResumeScheduledMessageWithOffset>();
            bus.Expect(b => b.Send(Arg<List<ResumeScheduledMessageWithOffset>>.Is.NotNull))
                .WhenCalled(i => resumeScheduledMessages = (List<ResumeScheduledMessageWithOffset>)((object[])(i.Arguments[0]))[0]);

            var scheduledMessageStatuses = new List<ScheduledMessageStatus> 
            {
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[0]}),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[1]}),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[2]}, MessageStatus.Sent)
            };
            var dateTime = DateTime.Now;
            var sagaData = new CoordinateSmsSchedulingData { ScheduledMessageStatus = scheduledMessageStatuses, OriginalScheduleStartTime = dateTime.AddMinutes(-5) };
            var smsScheduler = new CoordinateSmsScheduler { Bus = bus, Data = sagaData };
            
            var resumeTricklesMessages = new ResumeTrickledMessages { ResumeTime = dateTime };
            smsScheduler.Handle(resumeTricklesMessages);

            Assert.That(resumeScheduledMessages.Count, Is.EqualTo(2));
            Assert.That(resumeScheduledMessages[0].ScheduleMessageId, Is.EqualTo(scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId));
            Assert.That(resumeScheduledMessages[0].Offset, Is.EqualTo(new TimeSpan(0, 0, 5, 0)));
            Assert.That(resumeScheduledMessages[1].ScheduleMessageId, Is.EqualTo(scheduledMessageStatuses[1].ScheduledSms.ScheduleMessageId));
            Assert.That(resumeScheduledMessages[1].Offset, Is.EqualTo(new TimeSpan(0, 0, 5, 0)));

            Assert.That(scheduledMessageStatuses[0].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(scheduledMessageStatuses[1].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));

            timingManager.VerifyAllExpectations();
            bus.VerifyAllExpectations();
        }

        //[Test]
        //public void SmsScheduled_Data()
        //{
        //    var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj"), new SmsData("mobile", "sent") };

        //    var scheduledMessageStatuses = new List<ScheduledMessageStatus> 
        //    {
        //        new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[0]})
        //    };

        //    var sagaData = new CoordinateSmsSchedulingData { ScheduledMessageStatus = scheduledMessageStatuses };
        //    var smsScheduler = new CoordinateSmsScheduler { Data = sagaData };

        //    smsScheduler.Handle(new SmsScheduled { ScheduleMessageId = scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId });

        //    Assert.That(scheduledMessageStatuses[0].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
        //}
    }
}
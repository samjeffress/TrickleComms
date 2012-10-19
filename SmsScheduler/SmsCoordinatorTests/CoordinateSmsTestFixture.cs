using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.CommonData;
using SmsMessages.Coordinator;
using SmsMessages.MessageSending;
using SmsMessages.Scheduling;
using SmsMessages.Tracking;

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
                    .ExpectSend<CoordinatorCreated>()
                .When(s => s.Handle(trickleMultipleMessages))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m)}))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m) }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m) }))
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
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m) }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m) }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m) }))
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
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m) }))
                    .ExpectSend<List<PauseScheduledMessageIndefinitely>>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                    .ExpectSend<List<ResumeScheduledMessageWithOffset>>()
                .When(s => s.Handle(new ResumeTrickledMessages()))
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m) }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m) }))
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

            //var coordinatorCreated = new CoordinatorCreated();
            //var test = new CoordinatorCreated();
            //bus.Expect(b => b.Send(Arg<CoordinatorCreated>.Is.NotNull))
            //    .WhenCalled(i => test = ((object[])(i.Arguments[0]))[0] as CoordinatorCreated)
            //    //.WhenCalled(i => coordinatorCreated = (CoordinatorCreated)((object[])(i.Arguments[0]))[0])
            //    .Return(null);

            var scheduleSmsForLaterList = new List<ScheduleSmsForSendingLater>();
            var test2 = new List<ScheduleSmsForSendingLater>();
            bus.Expect(b => b.Send(Arg<List<ScheduleSmsForSendingLater>>.Is.NotNull))
                //.WhenCalled(j => test2 = ((object[])(j.Arguments[0]))[0] as List<ScheduleSmsForSendingLater>)
                .WhenCalled(i => scheduleSmsForLaterList = (List<ScheduleSmsForSendingLater>)((object[])(i.Arguments[0]))[0])
                .Return(null);

            //object test = new object();
            //bus.Expect(b => b.Send(Arg<CoordinatorCreated>.Is.NotNull))
            //    .WhenCalled(i => test = i.Arguments[0])
            //    .Return(null);

            var sagaData = new CoordinateSmsSchedulingData();
            var smsScheduler = new CoordinateSmsScheduler { Bus = bus, TimingManager = timingManager,Data = sagaData};
            smsScheduler.Handle(trickleMessagesOverTime);

            bus.VerifyAllExpectations();
            Assert.That(scheduleSmsForLaterList[0].SendMessageAt, Is.EqualTo(datetimeSpacing[0]));
            Assert.That(scheduleSmsForLaterList[0].SmsData.Message, Is.EqualTo(trickleMessagesOverTime.Messages[0].Message));
            Assert.That(scheduleSmsForLaterList[0].SmsData.Mobile, Is.EqualTo(trickleMessagesOverTime.Messages[0].Mobile));
            Assert.That(scheduleSmsForLaterList[0].SmsMetaData, Is.EqualTo(trickleMessagesOverTime.MetaData));

            Assert.That(scheduleSmsForLaterList[1].SendMessageAt, Is.EqualTo(datetimeSpacing[1]));
            Assert.That(scheduleSmsForLaterList[1].SmsData.Message, Is.EqualTo(trickleMessagesOverTime.Messages[1].Message));
            Assert.That(scheduleSmsForLaterList[1].SmsData.Mobile, Is.EqualTo(trickleMessagesOverTime.Messages[1].Mobile));
            Assert.That(scheduleSmsForLaterList[1].SmsMetaData, Is.EqualTo(trickleMessagesOverTime.MetaData));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            Assert.That(sagaData.ScheduledMessageStatus[0].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[0]));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            Assert.That(sagaData.ScheduledMessageStatus[1].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[1]));
            timingManager.VerifyAllExpectations();
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

            var coordinatorCreated = new CoordinatorCreated();
            bus.Expect(b => b.Send(Arg<CoordinatorCreated>.Is.NotNull))
                .WhenCalled(i => coordinatorCreated = (CoordinatorCreated)((object[])(i.Arguments[0]))[0]);

            var sagaData = new CoordinateSmsSchedulingData();
            var smsScheduler = new CoordinateSmsScheduler { Bus = bus, Data = sagaData };
            smsScheduler.Handle(trickleMessagesOverTime);
            bus.VerifyAllExpectations();

            Assert.That(scheduleSmsForLaterList[0].SendMessageAt.Ticks, Is.EqualTo(trickleMessagesOverTime.StartTime.Ticks));
            Assert.That(scheduleSmsForLaterList[0].SmsData.Message, Is.EqualTo(trickleMessagesOverTime.Messages[0].Message));
            Assert.That(scheduleSmsForLaterList[0].SmsData.Mobile, Is.EqualTo(trickleMessagesOverTime.Messages[0].Mobile));
            Assert.That(scheduleSmsForLaterList[0].SmsMetaData, Is.EqualTo(trickleMessagesOverTime.MetaData));

            Assert.That(scheduleSmsForLaterList[1].SendMessageAt.Ticks, Is.EqualTo(trickleMessagesOverTime.StartTime.Ticks + trickleMessagesOverTime.TimeSpacing.Ticks));
            Assert.That(scheduleSmsForLaterList[1].SmsData.Message, Is.EqualTo(trickleMessagesOverTime.Messages[1].Message));
            Assert.That(scheduleSmsForLaterList[1].SmsData.Mobile, Is.EqualTo(trickleMessagesOverTime.Messages[1].Mobile));
            Assert.That(scheduleSmsForLaterList[1].SmsMetaData, Is.EqualTo(trickleMessagesOverTime.MetaData));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            Assert.That(sagaData.ScheduledMessageStatus[0].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[0]));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            Assert.That(sagaData.ScheduledMessageStatus[1].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[1]));
            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessageScheduledSuccess_Data()
        {
            var scheduleMessageId = Guid.NewGuid();
            var smsScheduled = new SmsScheduled { ScheduleMessageId = scheduleMessageId };

            var bus = MockRepository.GenerateMock<IBus>();
            var coordinatorMessageScheduled = new CoordinatorMessageScheduled();
            bus.Expect(b => b.Send(Arg<CoordinatorMessageScheduled>.Is.Anything))
                .WhenCalled(i => coordinatorMessageScheduled = (CoordinatorMessageScheduled)((object[])(i.Arguments[0]))[0])
                .Return(null);

            var smsScheduledForLater = new ScheduleSmsForSendingLater(DateTime.Now.AddMinutes(10), new SmsData("mobile", "message"), new SmsMetaData()) { ScheduleMessageId = scheduleMessageId};
            var sagaData = new CoordinateSmsSchedulingData { ScheduledMessageStatus = new List<ScheduledMessageStatus>
            {
                new ScheduledMessageStatus(smsScheduledForLater)
            }};
            var smsScheduler = new CoordinateSmsScheduler { Data = sagaData, Bus = bus };
            smsScheduler.Handle(smsScheduled);

            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));

            Assert.That(coordinatorMessageScheduled.ScheduleMessageId, Is.EqualTo(smsScheduled.ScheduleMessageId));
            Assert.That(coordinatorMessageScheduled.Number, Is.EqualTo(smsScheduledForLater.SmsData.Mobile));
            Assert.That(coordinatorMessageScheduled.CoordinatorId, Is.EqualTo(smsScheduled.CoordinatorId));
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
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[1]}, MessageStatus.Scheduled),
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
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[0]}, MessageStatus.Paused),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[1]}, MessageStatus.Paused),
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

        [Test]
        public void ScheduledSmsSent_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj"), new SmsData("mobile", "sent") };
            var bus = MockRepository.GenerateMock<IBus>();
            var coordinatorMessageSent = new CoordinatorMessageSent();
            bus.Expect(b => b.Send(Arg<CoordinatorMessageSent>.Is.Anything))
                .WhenCalled(i => coordinatorMessageSent = (CoordinatorMessageSent)((object[])(i.Arguments[0]))[0])
                .Return(null);

            var scheduledMessageStatuses = new List<ScheduledMessageStatus> 
            {
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[0]})
            };

            var sagaData = new CoordinateSmsSchedulingData { ScheduledMessageStatus = scheduledMessageStatuses };
            var smsScheduler = new CoordinateSmsScheduler { Data = sagaData, Bus = bus };

            var scheduledSmsSent = new ScheduledSmsSent {ScheduledSmsId = scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId, ConfirmationData = new SmsConfirmationData("receipt", DateTime.Now, 3.44m)};
            smsScheduler.Handle(scheduledSmsSent);

            Assert.That(scheduledMessageStatuses[0].MessageStatus, Is.EqualTo(MessageStatus.Sent));
            Assert.That(coordinatorMessageSent.CoordinatorId, Is.EqualTo(scheduledSmsSent.CoordinatorId));
            Assert.That(coordinatorMessageSent.Cost, Is.EqualTo(scheduledSmsSent.ConfirmationData.Price));
            Assert.That(coordinatorMessageSent.Number, Is.EqualTo(scheduledSmsSent.Number));
            Assert.That(coordinatorMessageSent.ScheduleMessageId, Is.EqualTo(scheduledSmsSent.ScheduledSmsId));
            Assert.That(coordinatorMessageSent.TimeSent, Is.EqualTo(scheduledSmsSent.ConfirmationData.SentAt));

            bus.VerifyAllExpectations();
        }
    }
}
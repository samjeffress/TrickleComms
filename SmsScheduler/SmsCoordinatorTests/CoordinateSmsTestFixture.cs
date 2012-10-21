using System;
using System.Collections.Generic;
using NServiceBus;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.CommonData;
using SmsMessages.Coordinator;
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
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
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
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
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
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .ExpectSend<List<PauseScheduledMessageIndefinitely>>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                    .ExpectSend<List<ResumeScheduledMessageWithOffset>>()
                .When(s => s.Handle(new ResumeTrickledMessages()))
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectSend<CoordinatorMessageSent>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
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
            
            var datetimeSpacing = new List<DateTime> { DateTime.Now.AddMinutes(10), DateTime.Now.AddMinutes(20) };
            timingManager
                .Expect(t => t.CalculateTiming(trickleMessagesOverTime.StartTime, trickleMessagesOverTime.Duration, trickleMessagesOverTime.Messages.Count))
                .Return(datetimeSpacing);


            var sagaData = new CoordinateSmsSchedulingData { Originator = "originator", Id = Guid.NewGuid() };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                })
                    .ExpectSend<List<ScheduleSmsForSendingLater>>(l => 
                        l.Count == 2 && 
                        l[0].SendMessageAt == datetimeSpacing[0] &&
                        l[0].SmsData.Message == trickleMessagesOverTime.Messages[0].Message &&
                        l[0].SmsData.Mobile == trickleMessagesOverTime.Messages[0].Mobile &&
                        l[0].SmsMetaData == trickleMessagesOverTime.MetaData &&
                        
                        l[1].SendMessageAt == datetimeSpacing[1] &&
                        l[1].SmsData.Message == trickleMessagesOverTime.Messages[1].Message &&
                        l[1].SmsData.Mobile == trickleMessagesOverTime.Messages[1].Mobile &&
                        l[1].SmsMetaData == trickleMessagesOverTime.MetaData
                        )
                    .ExpectSend<CoordinatorCreated>(c => 
                        c.CoordinatorId == sagaData.Id && 
                        c.ScheduledMessages.Count == 2 &&
                        c.ScheduledMessages[0].Number == trickleMessagesOverTime.Messages[0].Mobile &&
                        c.ScheduledMessages[0].ScheduleMessageId == sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[0].ScheduleMessageId != Guid.Empty && 
                        c.ScheduledMessages[0].ScheduledTime == datetimeSpacing[0] &&

                        c.ScheduledMessages[1].Number == trickleMessagesOverTime.Messages[1].Mobile &&
                        c.ScheduledMessages[1].ScheduleMessageId == sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[1].ScheduleMessageId != Guid.Empty && 
                        c.ScheduledMessages[1].ScheduledTime == datetimeSpacing[1])
                .When(s => s.Handle(trickleMessagesOverTime));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            //Assert.That(sagaData.ScheduledMessageStatus[0].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[0]));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            //Assert.That(sagaData.ScheduledMessageStatus[1].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[1]));
            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesSpacedByTimespan_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj") };
            var trickleMessagesOverTime = new TrickleSmsSpacedByTimePeriod {  TimeSpacing = new TimeSpan(1000), Messages = messageList, StartTime = DateTime.Now };

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            
            var sagaData = new CoordinateSmsSchedulingData { Originator = "originator", Id = Guid.NewGuid() };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                })
                    .ExpectSend<List<ScheduleSmsForSendingLater>>(l =>
                        l.Count == 2 &&
                        l[0].SendMessageAt.Ticks == trickleMessagesOverTime.StartTime.Ticks &&
                        l[0].SmsData.Message == trickleMessagesOverTime.Messages[0].Message &&
                        l[0].SmsData.Mobile == trickleMessagesOverTime.Messages[0].Mobile &&
                        l[0].SmsMetaData == trickleMessagesOverTime.MetaData &&

                        l[1].SendMessageAt.Ticks == trickleMessagesOverTime.StartTime.Ticks + trickleMessagesOverTime.TimeSpacing.Ticks &&
                        l[1].SmsData.Message == trickleMessagesOverTime.Messages[1].Message &&
                        l[1].SmsData.Mobile == trickleMessagesOverTime.Messages[1].Mobile &&
                        l[1].SmsMetaData == trickleMessagesOverTime.MetaData
                        )
                    .ExpectSend<CoordinatorCreated>(c =>
                        c.CoordinatorId == sagaData.Id &&
                        c.ScheduledMessages.Count == 2 &&
                        c.ScheduledMessages[0].Number == trickleMessagesOverTime.Messages[0].Mobile &&
                        c.ScheduledMessages[0].ScheduleMessageId == sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[0].ScheduleMessageId != Guid.Empty && // HACK : Need to make this valid
                        c.ScheduledMessages[0].ScheduledTime.Ticks == trickleMessagesOverTime.StartTime.Ticks &&

                        c.ScheduledMessages[1].Number == trickleMessagesOverTime.Messages[1].Mobile &&
                        c.ScheduledMessages[1].ScheduleMessageId == sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[1].ScheduleMessageId != Guid.Empty && // HACK : Need to make this valid
                        c.ScheduledMessages[1].ScheduledTime.Ticks == trickleMessagesOverTime.StartTime.Ticks + trickleMessagesOverTime.TimeSpacing.Ticks)
                .When(s => s.Handle(trickleMessagesOverTime));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            //Assert.That(sagaData.ScheduledMessageStatus[0].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[0]));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            //Assert.That(sagaData.ScheduledMessageStatus[1].ScheduledSms, Is.EqualTo(scheduleSmsForLaterList[1]));
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

            var scheduledMessageStatuses = new List<ScheduledMessageStatus> 
            {
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[0]}),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[1]}, MessageStatus.Scheduled),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[2]}, MessageStatus.Sent)
            };

            var sagaData = new CoordinateSmsSchedulingData { ScheduledMessageStatus = scheduledMessageStatuses, Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                })
                    .ExpectSend<List<PauseScheduledMessageIndefinitely>>(
                        l => l.Count == 2 && 
                        l[0].ScheduleMessageId == scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId &&
                        l[1].ScheduleMessageId == scheduledMessageStatuses[1].ScheduledSms.ScheduleMessageId)
                .When(s => s.Handle(pauseMessageSending));

            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.Paused));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.Paused));

            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesResumeMessageSending_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj"), new SmsData("mobile", "sent") };

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();

            var scheduledMessageStatuses = new List<ScheduledMessageStatus> 
            {
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[0]}, MessageStatus.Paused),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[1]}, MessageStatus.Paused),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[2]}, MessageStatus.Sent)
            };
            var dateTime = DateTime.Now;
            var sagaData = new CoordinateSmsSchedulingData { Originator = "o", ScheduledMessageStatus = scheduledMessageStatuses, OriginalScheduleStartTime = dateTime.AddMinutes(-5) };
            
            var resumeTricklesMessages = new ResumeTrickledMessages { ResumeTime = dateTime };

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                })
                    .ExpectSend<List<ResumeScheduledMessageWithOffset>>(
                        l => l.Count == 2 &&
                        l[0].ScheduleMessageId == scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId &&
                        l[0].Offset == new TimeSpan(0, 0, 5, 0) &&
                        l[1].ScheduleMessageId == scheduledMessageStatuses[1].ScheduledSms.ScheduleMessageId &&
                        l[0].Offset == new TimeSpan(0, 0, 5, 0))
                .When(s => s.Handle(resumeTricklesMessages));

            Assert.That(scheduledMessageStatuses[0].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(scheduledMessageStatuses[1].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));

            timingManager.VerifyAllExpectations();
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
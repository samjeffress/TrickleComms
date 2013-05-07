using System;
using System.Collections.Generic;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsMessages.Coordinator.Events;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Scheduling.Events;

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
            var trickleMultipleMessages = new TrickleSmsOverCalculatedIntervalsBetweenSetDates
            {
                StartTimeUtc = startTime,
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
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectPublish<CoordinatorCreated>()
                .When(s => s.Handle(trickleMultipleMessages))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(true);

            timingManager.VerifyAllExpectations();
            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSentOrFailed, Is.EqualTo(3));
        }

        [Test]
        public void TrickleThreeMessagesOverTenMinutesOneMessageFailsCoordinatorStillCompletes()
        {
            var startTime = DateTime.Now.AddHours(3);
            var duration = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsOverCalculatedIntervalsBetweenSetDates
            {
                StartTimeUtc = startTime,
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
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectPublish<CoordinatorCreated>()
                .When(s => s.Handle(trickleMultipleMessages))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Handle(new ScheduledSmsFailed { ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(true);

            timingManager.VerifyAllExpectations();
            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSentOrFailed, Is.EqualTo(3));
        }

        [Test]
        public void TrickleThreeMessagesSpacedAMinuteApart()
        {
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                StartTimeUtc = startTime,
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
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                .When(s => s.Handle(trickleMultipleMessages))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(true);

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSentOrFailed, Is.EqualTo(3));
        }

        [Test]
        public void TrickleThreeMessagesSpacedAMinuteApartPausedAfterFirstThenResumed()
        {
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                StartTimeUtc = startTime,
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
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                .When(s => s.Handle(trickleMultipleMessages))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                .When(s => s.Handle(new MessageSchedulePaused { ScheduleId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                .When(s => s.Handle(new MessageSchedulePaused { ScheduleId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                .When(s => s.Handle(new ResumeTrickledMessages()))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(true);

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSentOrFailed, Is.EqualTo(3));
        }

        [Test]
        public void TrickleThreeMessagesSpacedAMinuteApartPausedAfterFirstThenRescheduled()
        {
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                StartTimeUtc = startTime,
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
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                .When(s => s.Handle(trickleMultipleMessages))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                .When(s => s.Handle(new MessageSchedulePaused { ScheduleId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                .When(s => s.Handle(new MessageSchedulePaused { ScheduleId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                .When(s => s.Handle(new RescheduleTrickledMessages()))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(true);

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSentOrFailed, Is.EqualTo(3));
        }

        [Test]
        public void TrickleThreeMessagesPausedAfterFirstThenResumed()
        {
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                StartTimeUtc = startTime,
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
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                .When(s => s.Handle(trickleMultipleMessages))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                .When(s => s.Handle(new MessageSchedulePaused { ScheduleId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                .When(s => s.Handle(new MessageSchedulePaused { ScheduleId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                .When(s => s.Handle(new ResumeTrickledMessages()))
                .When(s => s.Handle(new MessageRescheduled { ScheduleMessageId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                .When(s => s.Handle(new MessageRescheduled { ScheduleMessageId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(true);

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSentOrFailed, Is.EqualTo(3));
        }
        
        [Test]
        public void TrickleThreeMessagesFirstSentPausedThenResumedOutOfOrder()
        {
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                StartTimeUtc = startTime,
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
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                .When(s => s.Handle(trickleMultipleMessages))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .ExpectNotSend<ResumeScheduledMessageWithOffset>(null)
                    .ExpectNotSend<ResumeScheduledMessageWithOffset>(null)
                .When(s => s.Handle(new ResumeTrickledMessages { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-10)}))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely { MessageRequestTimeUtc = DateTime.Now }))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(true);

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSentOrFailed, Is.EqualTo(3));
        }

        [Test]
        public void TrickleThreeMessagesFirstSentPausedThenResumed_SecondPauseMessageOutOfOrderIgnored()
        {
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var trickleMultipleMessages = new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                StartTimeUtc = startTime,
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
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                .When(s => s.Handle(trickleMultipleMessages))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId }))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-11) }))
                    .ExpectNotSend<ResumeScheduledMessageWithOffset>(null)
                    .ExpectNotSend<ResumeScheduledMessageWithOffset>(null)
                .When(s => s.Handle(new ResumeTrickledMessages { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-10)}))
                    .ExpectNotSend<PauseScheduledMessageIndefinitely>(null)
                    .ExpectNotSend<PauseScheduledMessageIndefinitely>(null)
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-11) }))
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Handle(new ScheduledSmsSent { ConfirmationData = new SmsConfirmationData("r", DateTime.Now, 1m), ScheduledSmsId = sagaData.ScheduledMessageStatus[2].ScheduledSms.ScheduleMessageId }))
                    .AssertSagaCompletionIs(true);

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(3));
            Assert.That(sagaData.MessagesConfirmedSentOrFailed, Is.EqualTo(3));
        }

        [Test]
        public void TrickleMessagesOverPeriod_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj")};
            var trickleMessagesOverTime = new TrickleSmsOverCalculatedIntervalsBetweenSetDates { Duration = new TimeSpan(1000), Messages = messageList, StartTimeUtc = DateTime.Now, UserOlsenTimeZone = "timeZone"};

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            
            var datetimeSpacing = new List<DateTime> { DateTime.Now.AddMinutes(10), DateTime.Now.AddMinutes(20) };
            timingManager
                .Expect(t => t.CalculateTiming(trickleMessagesOverTime.StartTimeUtc, trickleMessagesOverTime.Duration, trickleMessagesOverTime.Messages.Count))
                .Return(datetimeSpacing);


            var sagaData = new CoordinateSmsSchedulingData { Originator = "originator", Id = Guid.NewGuid() };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => 
                        l.SendMessageAtUtc == datetimeSpacing[0] &&
                        l.SmsData.Message == trickleMessagesOverTime.Messages[0].Message &&
                        l.SmsData.Mobile == trickleMessagesOverTime.Messages[0].Mobile &&
                        l.SmsMetaData == trickleMessagesOverTime.MetaData)
                    .ExpectSend<ScheduleSmsForSendingLater>(l =>     
                        l.SendMessageAtUtc == datetimeSpacing[1] &&
                        l.SmsData.Message == trickleMessagesOverTime.Messages[1].Message &&
                        l.SmsData.Mobile == trickleMessagesOverTime.Messages[1].Mobile &&
                        l.SmsMetaData == trickleMessagesOverTime.MetaData)
                    .ExpectPublish<CoordinatorCreated>(c => 
                        c.CoordinatorId == sagaData.Id && 
                        c.ScheduledMessages.Count == 2 &&
                        c.ScheduledMessages[0].Number == trickleMessagesOverTime.Messages[0].Mobile &&
                        c.ScheduledMessages[0].ScheduleMessageId == sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[0].ScheduleMessageId != Guid.Empty && 
                        c.ScheduledMessages[0].ScheduledTimeUtc == datetimeSpacing[0] &&

                        c.ScheduledMessages[1].Number == trickleMessagesOverTime.Messages[1].Mobile &&
                        c.ScheduledMessages[1].ScheduleMessageId == sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[1].ScheduleMessageId != Guid.Empty && 
                        c.ScheduledMessages[1].ScheduledTimeUtc == datetimeSpacing[1] &&
                        c.UserOlsenTimeZone == trickleMessagesOverTime.UserOlsenTimeZone)
                .When(s => s.Handle(trickleMessagesOverTime));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void SendAllAtOnce_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj")};
            var sendTimeUtc = DateTime.Now;
            var sendAllMessagesAtOnce = new SendAllMessagesAtOnce { Messages = messageList, SendTimeUtc = sendTimeUtc, UserOlsenTimeZone = "timeZone"};

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();

            var sagaData = new CoordinateSmsSchedulingData { Originator = "originator", Id = Guid.NewGuid() };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => 
                        l.SendMessageAtUtc == sendTimeUtc &&
                        l.SmsData.Message == sendAllMessagesAtOnce.Messages[0].Message &&
                        l.SmsData.Mobile == sendAllMessagesAtOnce.Messages[0].Mobile &&
                        l.SmsMetaData == sendAllMessagesAtOnce.MetaData)
                    .ExpectSend<ScheduleSmsForSendingLater>(l =>
                        l.SendMessageAtUtc == sendTimeUtc &&
                        l.SmsData.Message == sendAllMessagesAtOnce.Messages[1].Message &&
                        l.SmsData.Mobile == sendAllMessagesAtOnce.Messages[1].Mobile &&
                        l.SmsMetaData == sendAllMessagesAtOnce.MetaData)
                    .ExpectPublish<CoordinatorCreated>(c => 
                        c.CoordinatorId == sagaData.Id && 
                        c.ScheduledMessages.Count == 2 &&
                        c.ScheduledMessages[0].Number == sendAllMessagesAtOnce.Messages[0].Mobile &&
                        c.ScheduledMessages[0].ScheduleMessageId == sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[0].ScheduleMessageId != Guid.Empty &&
                        c.ScheduledMessages[0].ScheduledTimeUtc == sendTimeUtc &&

                        c.ScheduledMessages[1].Number == sendAllMessagesAtOnce.Messages[1].Mobile &&
                        c.ScheduledMessages[1].ScheduleMessageId == sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[1].ScheduleMessageId != Guid.Empty &&
                        c.ScheduledMessages[1].ScheduledTimeUtc == sendTimeUtc &&
                        c.UserOlsenTimeZone == sendAllMessagesAtOnce.UserOlsenTimeZone)
                .When(s => s.Handle(sendAllMessagesAtOnce));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesSpacedByTimespan_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj") };
            var trickleMessagesOverTime = new TrickleSmsWithDefinedTimeBetweenEachMessage {  TimeSpacing = new TimeSpan(1000), Messages = messageList, StartTimeUtc = DateTime.Now, UserOlsenTimeZone = "timeZone"};

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            
            var sagaData = new CoordinateSmsSchedulingData { Originator = "originator", Id = Guid.NewGuid() };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                })
                    .ExpectSend<ScheduleSmsForSendingLater>(l =>
                        //l.Count == 2 &&
                        l.SendMessageAtUtc.Ticks == trickleMessagesOverTime.StartTimeUtc.Ticks &&
                        l.SmsData.Message == trickleMessagesOverTime.Messages[0].Message &&
                        l.SmsData.Mobile == trickleMessagesOverTime.Messages[0].Mobile &&
                        l.SmsMetaData == trickleMessagesOverTime.MetaData)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => 
                        l.SendMessageAtUtc.Ticks == trickleMessagesOverTime.StartTimeUtc.Ticks + trickleMessagesOverTime.TimeSpacing.Ticks &&
                        l.SmsData.Message == trickleMessagesOverTime.Messages[1].Message &&
                        l.SmsData.Mobile == trickleMessagesOverTime.Messages[1].Mobile &&
                        l.SmsMetaData == trickleMessagesOverTime.MetaData)
                    .ExpectPublish<CoordinatorCreated>(c =>
                        c.CoordinatorId == sagaData.Id &&
                        c.ScheduledMessages.Count == 2 &&
                        c.ScheduledMessages[0].Number == trickleMessagesOverTime.Messages[0].Mobile &&
                        c.ScheduledMessages[0].ScheduleMessageId == sagaData.ScheduledMessageStatus[0].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[0].ScheduleMessageId != Guid.Empty && // HACK : Need to make this valid
                        c.ScheduledMessages[0].ScheduledTimeUtc.Ticks == trickleMessagesOverTime.StartTimeUtc.Ticks &&

                        c.ScheduledMessages[1].Number == trickleMessagesOverTime.Messages[1].Mobile &&
                        c.ScheduledMessages[1].ScheduleMessageId == sagaData.ScheduledMessageStatus[1].ScheduledSms.ScheduleMessageId && 
                        c.ScheduledMessages[1].ScheduleMessageId != Guid.Empty && // HACK : Need to make this valid
                        c.ScheduledMessages[1].ScheduledTimeUtc.Ticks == trickleMessagesOverTime.StartTimeUtc.Ticks + trickleMessagesOverTime.TimeSpacing.Ticks &&
                        c.UserOlsenTimeZone == trickleMessagesOverTime.UserOlsenTimeZone)
                .When(s => s.Handle(trickleMessagesOverTime));

            Assert.That(sagaData.MessagesScheduled, Is.EqualTo(2));
            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            timingManager.VerifyAllExpectations();
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
                    .ExpectSend<PauseScheduledMessageIndefinitely>(
                        l => l.ScheduleMessageId == scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId)
                    .ExpectSend<PauseScheduledMessageIndefinitely>(l => 
                        l.ScheduleMessageId == scheduledMessageStatuses[1].ScheduledSms.ScheduleMessageId)
                .When(s => s.Handle(pauseMessageSending));

            Assert.That(sagaData.ScheduledMessageStatus[0].MessageStatus, Is.EqualTo(MessageStatus.WaitingForScheduling));
            Assert.That(sagaData.ScheduledMessageStatus[1].MessageStatus, Is.EqualTo(MessageStatus.Scheduled));

            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesMessagesPausedConfirmedButAlreadySentThrowsException_Data()
        {
            var messageList = new SmsData("99999", "dj;alsdfkj");

            var scheduleId = Guid.NewGuid();
            var messageSchedulePaused = new MessageSchedulePaused { ScheduleId = scheduleId };

            var scheduledMessageStatuses = new List<ScheduledMessageStatus> 
            {
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList, ScheduleMessageId = scheduleId }, MessageStatus.Sent)
            };

            var sagaData = new CoordinateSmsSchedulingData { ScheduledMessageStatus = scheduledMessageStatuses, Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };

            var coordinateSmsScheduler = new CoordinateSmsScheduler {Data = sagaData};
            Assert.That(() => coordinateSmsScheduler.Handle(messageSchedulePaused), Throws.Exception.With.Message.EqualTo("Scheduled message " + scheduleId + " is already sent."));
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
            
            var resumeTricklesMessages = new ResumeTrickledMessages { ResumeTimeUtc = dateTime };

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                })
                    .ExpectSend<ResumeScheduledMessageWithOffset>(
                        l => 
                        l.ScheduleMessageId == scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId &&
                        l.Offset == new TimeSpan(0, 0, 5, 0))
                    .ExpectSend<ResumeScheduledMessageWithOffset>(
                        l =>
                        l.ScheduleMessageId == scheduledMessageStatuses[1].ScheduledSms.ScheduleMessageId &&
                        l.Offset == new TimeSpan(0, 0, 5, 0))
                .When(s => s.Handle(resumeTricklesMessages));

            Assert.That(scheduledMessageStatuses[0].MessageStatus, Is.EqualTo(MessageStatus.Paused));
            Assert.That(scheduledMessageStatuses[1].MessageStatus, Is.EqualTo(MessageStatus.Paused));

            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesRescheduleMessageSending_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj"), new SmsData("mobile", "sent") };

            var scheduledMessageStatuses = new List<ScheduledMessageStatus> 
            {
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[0]}, MessageStatus.Paused),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[1]}, MessageStatus.Paused),
                new ScheduledMessageStatus(new ScheduleSmsForSendingLater { SmsData = messageList[2]}, MessageStatus.Sent)
            };
            var dateTime = DateTime.Now;
            var finishTime = dateTime.AddMinutes(9);
            var sagaData = new CoordinateSmsSchedulingData { Originator = "o", ScheduledMessageStatus = scheduledMessageStatuses, OriginalScheduleStartTime = dateTime.AddMinutes(-5) };
            
            var rescheduleTrickledMessages = new RescheduleTrickledMessages { ResumeTimeUtc = dateTime, FinishTimeUtc = finishTime};

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.Data = sagaData;
                })
                    .ExpectSend<ResumeScheduledMessageWithOffset>(
                        l => 
                        l.ScheduleMessageId == scheduledMessageStatuses[0].ScheduledSms.ScheduleMessageId &&
                        l.Offset == new TimeSpan(0, 0, 5, 0))
                    .ExpectSend<ResumeScheduledMessageWithOffset>(
                        l =>
                        l.ScheduleMessageId == scheduledMessageStatuses[1].ScheduledSms.ScheduleMessageId &&
                        l.Offset == new TimeSpan(0, 0, 5, 0))
                .When(s => s.Handle(rescheduleTrickledMessages));

            Assert.That(scheduledMessageStatuses[0].MessageStatus, Is.EqualTo(MessageStatus.Paused));
            Assert.That(scheduledMessageStatuses[1].MessageStatus, Is.EqualTo(MessageStatus.Paused));
        }
    }
}
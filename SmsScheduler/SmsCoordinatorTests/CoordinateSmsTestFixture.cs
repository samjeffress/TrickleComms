using System;
using System.Collections.Generic;
using NServiceBus.Testing;
using NUnit.Framework;
using Rhino.Mocks;
using SmsCoordinator;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsMessages.Coordinator.Events;
using SmsMessages.Email.Commands;
using SmsMessages.Scheduling.Commands;
using SmsTrackingMessages.Messages;
using SmsTrackingModels;

namespace SmsCoordinatorTests
{
    [TestFixture]
    public class CoordinateSmsTestFixture
    {        
        [Test]
        public void TrickleThreeMessagesOverTenMinutes()
        {
            var coordinatorId = Guid.NewGuid();
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            var sendingMessages = new List<ScheduleSmsForSendingLater>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.NotNull, Arg<Guid>.Is.Equal(coordinatorId)))
                .WhenCalled(r => sendingMessages = (List<ScheduleSmsForSendingLater>)r.Arguments[0]);
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.MarkCoordinatorAsComplete(Arg<Guid>.Is.Equal(coordinatorId), Arg<DateTime>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());
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
                Duration = duration,
                CoordinatorId = coordinatorId,
                MetaData = new SmsMetaData()
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
                        s.RavenScheduleDocuments = ravenScheduleDocuments;
                    })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectPublish<CoordinatorCreated>()
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => true)
                .When(s => s.Handle(trickleMultipleMessages))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Timeout(new CoordinatorTimeout()))    
                .AssertSagaCompletionIs(true);

            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleThreeMessagesOverTenMinutesOneMessageFailsCoordinatorStillCompletes()
        {
            var coordinatorId = Guid.NewGuid();
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.Anything, Arg<Guid>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.MarkCoordinatorAsComplete(Arg<Guid>.Is.Equal(coordinatorId), Arg<DateTime>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());
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
                Duration = duration,
                CoordinatorId = coordinatorId,
                MetaData = new SmsMetaData()
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
                        s.RavenScheduleDocuments = ravenScheduleDocuments;
                    })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectPublish<CoordinatorCreated>()
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => true)
                .When(s => s.Handle(trickleMultipleMessages))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(true);

            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleThreeMessagesSpacedAMinuteApart()
        {
            var coordinatorId = Guid.NewGuid();
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.Anything, Arg<Guid>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.MarkCoordinatorAsComplete(Arg<Guid>.Is.Equal(coordinatorId), Arg<DateTime>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());
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
                TimeSpacing = timeSpacing,
                CoordinatorId = coordinatorId,
                MetaData = new SmsMetaData()
            };

            var sagaData = new CoordinateSmsSchedulingData {Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(d => { d.Data = sagaData; d.RavenScheduleDocuments = ravenScheduleDocuments; })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => true)
                .When(s => s.Handle(trickleMultipleMessages))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void TrickleThreeMessagesSpacedAMinuteApartPausedAfterFirstThenResumed()
        {
            var coordinatorId = Guid.NewGuid();
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.Anything, Arg<Guid>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.MarkCoordinatorAsComplete(Arg<Guid>.Is.Equal(coordinatorId), Arg<DateTime>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());

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
                TimeSpacing = timeSpacing,
                CoordinatorId = coordinatorId,
                MetaData = new SmsMetaData()
            };

            var sagaData = new CoordinateSmsSchedulingData { Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(d => { d.Data = sagaData; d.RavenScheduleDocuments = ravenScheduleDocuments; })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => true)
                .When(s => s.Handle(trickleMultipleMessages))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                .When(s => s.Handle(new ResumeTrickledMessages()))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void TrickleThreeMessagesSpacedAMinuteApartPausedAfterFirstThenRescheduled()
        {
            var coordinatorId = Guid.NewGuid();
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.Anything, Arg<Guid>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.MarkCoordinatorAsComplete(Arg<Guid>.Is.Equal(coordinatorId), Arg<DateTime>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());
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
                TimeSpacing = timeSpacing,
                CoordinatorId = coordinatorId,
                MetaData = new SmsMetaData()
            };

            var sagaData = new CoordinateSmsSchedulingData { Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(d => { d.Data = sagaData; d.RavenScheduleDocuments = ravenScheduleDocuments; })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => true)
                .When(s => s.Handle(trickleMultipleMessages))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                    .ExpectSend<RescheduleScheduledMessageWithNewTime>()
                    .ExpectSend<RescheduleScheduledMessageWithNewTime>()
                .When(s => s.Handle(new RescheduleTrickledMessages()))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void TrickleThreeMessagesPausedAfterFirstThenResumed()
        {
            var coordinatorId = Guid.NewGuid();
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.Anything, Arg<Guid>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.MarkCoordinatorAsComplete(Arg<Guid>.Is.Equal(coordinatorId), Arg<DateTime>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());
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
                TimeSpacing = timeSpacing,
                CoordinatorId = coordinatorId,
                MetaData = new SmsMetaData()
            };

            var sagaData = new CoordinateSmsSchedulingData { Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(d => { d.Data = sagaData; d.RavenScheduleDocuments = ravenScheduleDocuments; })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => true)
                .When(s => s.Handle(trickleMultipleMessages))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely()))
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                .When(s => s.Handle(new ResumeTrickledMessages()))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(true);
        }
        
        [Test]
        public void TrickleThreeMessagesFirstSentPausedThenResumedOutOfOrder()
        {
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var coordinatorId = Guid.NewGuid();
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.Anything, Arg<Guid>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.MarkCoordinatorAsComplete(Arg<Guid>.Is.Equal(coordinatorId), Arg<DateTime>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());
            var trickleMultipleMessages = new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                StartTimeUtc = startTime,
                Messages = new List<SmsData>
                {
                    new SmsData("mobile#1", "message"), 
                    new SmsData("mobile#2", "message2"),
                    new SmsData("mobile#3", "message3")
                },
                TimeSpacing = timeSpacing,
                CoordinatorId = coordinatorId,
                MetaData = new SmsMetaData()
            };

            var sagaData = new CoordinateSmsSchedulingData { Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(d => { d.Data = sagaData; d.RavenScheduleDocuments = ravenScheduleDocuments; })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => true)
                .When(s => s.Handle(trickleMultipleMessages))
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                .When(s => s.Handle(new ResumeTrickledMessages { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-10)}))
                    .ExpectNotSend<PauseScheduledMessageIndefinitely>(null)
                    .ExpectNotSend<PauseScheduledMessageIndefinitely>(null)
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-11) }))
                    .AssertSagaCompletionIs(false)
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void TrickleThreeMessagesFirstSentPausedThenResumed_SecondPauseMessageOutOfOrderIgnored()
        {
            var coordinatorId = Guid.NewGuid();
            var startTime = DateTime.Now.AddHours(3);
            var timeSpacing = new TimeSpan(0, 10, 0);
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.Anything, Arg<Guid>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData> { new ScheduleTrackingData(), new ScheduleTrackingData() });
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(coordinatorId)).Return(new List<ScheduleTrackingData>());
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.MarkCoordinatorAsComplete(Arg<Guid>.Is.Equal(coordinatorId), Arg<DateTime>.Is.Anything));
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());
            var trickleMultipleMessages = new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                StartTimeUtc = startTime,
                Messages = new List<SmsData>
                {
                    new SmsData("mobile#1", "message"), 
                    new SmsData("mobile#2", "message2"),
                    new SmsData("mobile#3", "message3")
                },
                TimeSpacing = timeSpacing,
                CoordinatorId = coordinatorId,
                MetaData = new SmsMetaData()
            };

            var sagaData = new CoordinateSmsSchedulingData { Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i" };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(d => { d.Data = sagaData; d.RavenScheduleDocuments = ravenScheduleDocuments; })
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[0].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[1].Mobile)
                    .ExpectSend<ScheduleSmsForSendingLater>(l => l.SmsData.Mobile == trickleMultipleMessages.Messages[2].Mobile)
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => true)
                .When(s => s.Handle(trickleMultipleMessages))
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                    .ExpectSend<PauseScheduledMessageIndefinitely>()
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-11) }))
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                    .ExpectSend<ResumeScheduledMessageWithOffset>()
                .When(s => s.Handle(new ResumeTrickledMessages { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-10) }))
                    .ExpectNotSend<PauseScheduledMessageIndefinitely>(null)
                    .ExpectNotSend<PauseScheduledMessageIndefinitely>(null)
                .When(s => s.Handle(new PauseTrickledMessagesIndefinitely { MessageRequestTimeUtc = DateTime.Now.AddMinutes(-11) }))
                    .ExpectPublish<CoordinatorCompleted>()
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(true);
        }

        [Test]
        public void TrickleMessagesOverPeriod_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj")};
            var trickleMessagesOverTime = new TrickleSmsOverCalculatedIntervalsBetweenSetDates { Duration = new TimeSpan(1000), Messages = messageList, StartTimeUtc = DateTime.Now, UserOlsenTimeZone = "timeZone", CoordinatorId = Guid.NewGuid(), MetaData = new SmsMetaData() };

            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            var sendingMessages = new List<ScheduleSmsForSendingLater>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.NotNull, Arg<Guid>.Is.Equal(trickleMessagesOverTime.CoordinatorId)))
                .WhenCalled(r => sendingMessages = (List<ScheduleSmsForSendingLater>)r.Arguments[0]);
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));

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
                    s.RavenScheduleDocuments = ravenScheduleDocuments;
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
                        c.CoordinatorId == trickleMessagesOverTime.CoordinatorId && 
                        c.ScheduledMessages.Count == 2 &&
                        c.ScheduledMessages[0].Number == trickleMessagesOverTime.Messages[0].Mobile &&
                        c.ScheduledMessages[0].ScheduleMessageId == sendingMessages[0].ScheduleMessageId && 
                        c.ScheduledMessages[0].ScheduleMessageId != Guid.Empty && 
                        c.ScheduledMessages[0].ScheduledTimeUtc == datetimeSpacing[0] &&

                        c.ScheduledMessages[1].Number == trickleMessagesOverTime.Messages[1].Mobile &&
                        c.ScheduledMessages[1].ScheduleMessageId == sendingMessages[1].ScheduleMessageId && 
                        c.ScheduledMessages[1].ScheduleMessageId != Guid.Empty && 
                        c.ScheduledMessages[1].ScheduledTimeUtc == datetimeSpacing[1] &&
                        c.UserOlsenTimeZone == trickleMessagesOverTime.UserOlsenTimeZone)
                .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => timeout == trickleMessagesOverTime.StartTimeUtc.Add(trickleMessagesOverTime.Duration).AddMinutes(2))
                .When(s => s.Handle(trickleMessagesOverTime));

            Assert.That(sendingMessages.Count, Is.EqualTo(2));
            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void SendAllAtOnce_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj")};
            var sendTimeUtc = DateTime.Now;
            var sendAllMessagesAtOnce = new SendAllMessagesAtOnce { Messages = messageList, SendTimeUtc = sendTimeUtc, UserOlsenTimeZone = "timeZone", CoordinatorId = Guid.NewGuid(), MetaData = new SmsMetaData()};

            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            var sendingMessages = new List<ScheduleSmsForSendingLater>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.NotNull, Arg<Guid>.Is.Equal(sendAllMessagesAtOnce.CoordinatorId)))
                .WhenCalled(r => sendingMessages = (List<ScheduleSmsForSendingLater>)r.Arguments[0]);
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();

            var sagaData = new CoordinateSmsSchedulingData { Originator = "originator", Id = Guid.NewGuid() };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                    s.RavenScheduleDocuments = ravenScheduleDocuments;
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
                        c.CoordinatorId == sendAllMessagesAtOnce.CoordinatorId && 
                        c.ScheduledMessages.Count == 2 &&
                        c.ScheduledMessages[0].Number == sendAllMessagesAtOnce.Messages[0].Mobile &&
                        c.ScheduledMessages[0].ScheduleMessageId == sendingMessages[0].ScheduleMessageId && 
                        c.ScheduledMessages[0].ScheduleMessageId != Guid.Empty &&
                        c.ScheduledMessages[0].ScheduledTimeUtc == sendTimeUtc &&

                        c.ScheduledMessages[1].Number == sendAllMessagesAtOnce.Messages[1].Mobile &&
                        c.ScheduledMessages[1].ScheduleMessageId == sendingMessages[1].ScheduleMessageId && 
                        c.ScheduledMessages[1].ScheduleMessageId != Guid.Empty &&
                        c.ScheduledMessages[1].ScheduledTimeUtc == sendTimeUtc &&
                        c.UserOlsenTimeZone == sendAllMessagesAtOnce.UserOlsenTimeZone)
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => timeout == sendAllMessagesAtOnce.SendTimeUtc.AddMinutes(2))
                .When(s => s.Handle(sendAllMessagesAtOnce));

            Assert.That(sendingMessages.Count, Is.EqualTo(2));
            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesSpacedByTimespan_Data()
        {
            var messageList = new List<SmsData> { new SmsData("9384938", "3943lasdkf;j"), new SmsData("99999", "dj;alsdfkj") };
            var trickleMessagesOverTime = new TrickleSmsWithDefinedTimeBetweenEachMessage {  TimeSpacing = new TimeSpan(1000), Messages = messageList, StartTimeUtc = DateTime.Now, UserOlsenTimeZone = "timeZone", CoordinatorId = Guid.NewGuid(), MetaData = new SmsMetaData()};
            var ravenScheduleDocuments = MockRepository.GenerateStrictMock<IRavenScheduleDocuments>();
            var sendingMessages = new List<ScheduleSmsForSendingLater>();
            ravenScheduleDocuments.Expect(r => r.SaveSchedules(Arg<List<ScheduleSmsForSendingLater>>.Is.NotNull, Arg<Guid>.Is.Equal(trickleMessagesOverTime.CoordinatorId)))
                .WhenCalled(r => sendingMessages = (List<ScheduleSmsForSendingLater>)r.Arguments[0]);
            ravenScheduleDocuments.Expect(r => r.SaveCoordinator(Arg<CoordinatorCreated>.Is.Anything));

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            
            var sagaData = new CoordinateSmsSchedulingData { Originator = "originator", Id = Guid.NewGuid() };
            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                    s.RavenScheduleDocuments = ravenScheduleDocuments;
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
                        c.CoordinatorId == trickleMessagesOverTime.CoordinatorId &&
                        c.ScheduledMessages.Count == 2 &&
                        c.ScheduledMessages[0].Number == trickleMessagesOverTime.Messages[0].Mobile &&
                        c.ScheduledMessages[0].ScheduleMessageId == sendingMessages[0].ScheduleMessageId && 
                        c.ScheduledMessages[0].ScheduleMessageId != Guid.Empty && // HACK : Need to make this valid
                        c.ScheduledMessages[0].ScheduledTimeUtc.Ticks == trickleMessagesOverTime.StartTimeUtc.Ticks &&

                        c.ScheduledMessages[1].Number == trickleMessagesOverTime.Messages[1].Mobile &&
                        c.ScheduledMessages[1].ScheduleMessageId == sendingMessages[1].ScheduleMessageId && 
                        c.ScheduledMessages[1].ScheduleMessageId != Guid.Empty && // HACK : Need to make this valid
                        c.ScheduledMessages[1].ScheduledTimeUtc.Ticks == trickleMessagesOverTime.StartTimeUtc.Ticks + trickleMessagesOverTime.TimeSpacing.Ticks &&
                        c.UserOlsenTimeZone == trickleMessagesOverTime.UserOlsenTimeZone)
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => timeout == trickleMessagesOverTime.StartTimeUtc.Add(trickleMessagesOverTime.TimeSpacing).AddMinutes(2))
                .When(s => s.Handle(trickleMessagesOverTime));

            Assert.That(sendingMessages.Count, Is.EqualTo(2));
            timingManager.VerifyAllExpectations();
            ravenScheduleDocuments.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesPauseMessagesIndefinitely_Data()
        {
            var ravenScheduleDocuments = MockRepository.GenerateMock<IRavenScheduleDocuments>();

            var pauseMessageSending = new PauseTrickledMessagesIndefinitely();

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();

            var sagaId = Guid.NewGuid();
            var sagaData = new CoordinateSmsSchedulingData { Id = Guid.NewGuid(), Originator = "o", OriginalMessageId = "i", CoordinatorId = sagaId };
            var pausedTrackedMessages = new List<ScheduleTrackingData>
                                            {
                                                new ScheduleTrackingData { MessageStatus = MessageStatus.Scheduled, ScheduleId = Guid.NewGuid()},
                                                new ScheduleTrackingData { MessageStatus = MessageStatus.Scheduled, ScheduleId = Guid.NewGuid()}
                                            };
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(sagaId)).Return(pausedTrackedMessages);

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                    s.RavenScheduleDocuments = ravenScheduleDocuments;
                })
                    .ExpectSend<PauseScheduledMessageIndefinitely>(l => l.ScheduleMessageId == pausedTrackedMessages[0].ScheduleId)
                    .ExpectSend<PauseScheduledMessageIndefinitely>(l => l.ScheduleMessageId == pausedTrackedMessages[1].ScheduleId)
                .When(s => s.Handle(pauseMessageSending));

            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesResumeMessageSending_Data()
        {
            var ravenScheduleDocuments = MockRepository.GenerateMock<IRavenScheduleDocuments>();

            var timingManager = MockRepository.GenerateMock<ICalculateSmsTiming>();
            var dateTime = DateTime.Now;
            var sagaId = Guid.NewGuid();
            var sagaData = new CoordinateSmsSchedulingData { Originator = "o", OriginalScheduleStartTime = dateTime.AddMinutes(-5), CoordinatorId = sagaId };
            var pausedTrackedMessages = new List<ScheduleTrackingData>
                                            {
                                                new ScheduleTrackingData { MessageStatus = MessageStatus.Paused, ScheduleId = Guid.NewGuid()},
                                                new ScheduleTrackingData { MessageStatus = MessageStatus.Paused, ScheduleId = Guid.NewGuid()}
                                            };
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(sagaId)).Return(pausedTrackedMessages);

            var resumeTricklesMessages = new ResumeTrickledMessages { ResumeTimeUtc = dateTime };

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.TimingManager = timingManager;
                    s.Data = sagaData;
                    s.RavenScheduleDocuments = ravenScheduleDocuments;
                })
                    .ExpectSend<ResumeScheduledMessageWithOffset>(
                        l => 
                        l.ScheduleMessageId == pausedTrackedMessages[0].ScheduleId &&
                        l.Offset == new TimeSpan(0, 0, 5, 0))
                    .ExpectSend<ResumeScheduledMessageWithOffset>(
                        l =>
                        l.ScheduleMessageId == pausedTrackedMessages[1].ScheduleId &&
                        l.Offset == new TimeSpan(0, 0, 5, 0))
                .When(s => s.Handle(resumeTricklesMessages));

            timingManager.VerifyAllExpectations();
        }

        [Test]
        public void TrickleMessagesRescheduleMessageSending_Data()
        {
            var ravenScheduleDocuments = MockRepository.GenerateMock<IRavenScheduleDocuments>();

            var dateTime = DateTime.Now;
            var finishTime = dateTime.AddMinutes(9);
            var sagaId = Guid.NewGuid();
            var sagaData = new CoordinateSmsSchedulingData { Originator = "o", OriginalScheduleStartTime = dateTime.AddMinutes(-5), CoordinatorId = sagaId};
            var pausedTrackedMessages = new List<ScheduleTrackingData>
                                            {
                                                new ScheduleTrackingData { MessageStatus = MessageStatus.Paused, ScheduleId = Guid.NewGuid()},
                                                new ScheduleTrackingData { MessageStatus = MessageStatus.Paused, ScheduleId = Guid.NewGuid()}
                                            };
            ravenScheduleDocuments.Expect(r => r.GetActiveScheduleTrackingData(sagaId)).Return(pausedTrackedMessages);

            var rescheduleTrickledMessages = new RescheduleTrickledMessages { ResumeTimeUtc = dateTime, FinishTimeUtc = finishTime};

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.Data = sagaData; s.RavenScheduleDocuments = ravenScheduleDocuments;
                })
                    .ExpectSend<RescheduleScheduledMessageWithNewTime>(
                        l => 
                        l.ScheduleMessageId == pausedTrackedMessages[0].ScheduleId &&
                        l.NewScheduleTimeUtc.Equals(dateTime))
                    .ExpectSend<RescheduleScheduledMessageWithNewTime>(
                        l =>
                        l.ScheduleMessageId == pausedTrackedMessages[1].ScheduleId &&
                        l.NewScheduleTimeUtc.Equals(finishTime))
                .When(s => s.Handle(rescheduleTrickledMessages));
        }

        [Test]
        public void CoordinatorTimeoutMessagesNotYetAllSentRequestsAnotherTimeoutUsingMaxExpectedScheduleDate()
        {
            var ravenScheduleDocuments = MockRepository.GenerateMock<IRavenScheduleDocuments>();
            var coordinatorId = Guid.NewGuid();
            var sagaData = new CoordinateSmsSchedulingData { Originator = "o", CoordinatorId = coordinatorId };

            var maxScheduleDateTime = DateTime.Now.AddMinutes(5);
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(false);
            ravenScheduleDocuments.Expect(r => r.GetMaxScheduleDateTime(coordinatorId)).Return(maxScheduleDateTime);

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.Data = sagaData; s.RavenScheduleDocuments = ravenScheduleDocuments;
                })
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => timeout == maxScheduleDateTime.AddMinutes(2))
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(false);
        }

        [Test]
        public void CoordinatorTimeoutMessagesNotYetAllSentRequestsAnotherTimeoutFor2MinutesAsMaxExpectedScheduleDateHasPassed()
        {
            var ravenScheduleDocuments = MockRepository.GenerateMock<IRavenScheduleDocuments>();
            var coordinatorId = Guid.NewGuid();
            var sagaData = new CoordinateSmsSchedulingData { Originator = "o", CoordinatorId = coordinatorId };

            var maxScheduleDateTime = DateTime.Now.AddMinutes(-1);
            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(false);
            ravenScheduleDocuments.Expect(r => r.GetMaxScheduleDateTime(coordinatorId)).Return(maxScheduleDateTime);

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.Data = sagaData; s.RavenScheduleDocuments = ravenScheduleDocuments;
                })
                // NOTE: Test is super brittle because of DateTime.Now - ideally this would be mocked
                    .ExpectTimeoutToBeSetAt<CoordinatorTimeout>((state, timeout) => timeout > DateTime.UtcNow.AddMinutes(1) && timeout < DateTime.UtcNow.AddMinutes(2))
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(false);
        }

        [Test]
        public void CoordinatorTimeoutMessagesAllSentSagaCompletes()
        {
            var ravenScheduleDocuments = MockRepository.GenerateMock<IRavenScheduleDocuments>();
            var coordinatorId = Guid.NewGuid();
            var sagaData = new CoordinateSmsSchedulingData { Originator = "o", CoordinatorId = coordinatorId };

            ravenScheduleDocuments.Expect(r => r.AreCoordinatedSchedulesComplete(coordinatorId)).Return(true);
            ravenScheduleDocuments.Expect(r => r.GetScheduleSummary(coordinatorId)).Return(new List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>());

            Test.Initialize();
            Test.Saga<CoordinateSmsScheduler>()
                .WithExternalDependencies(s =>
                {
                    s.Data = sagaData; s.RavenScheduleDocuments = ravenScheduleDocuments;
                })
                    .ExpectPublish<CoordinatorCompleted>()
                    .ExpectSend<CoordinatorCompleteEmailWithSummary>()
                .When(s => s.Timeout(new CoordinatorTimeout()))
                .AssertSagaCompletionIs(true);
        }
    }
}
using System;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.Scheduling.Commands;
using SmsMessages.Tracking.Scheduling.Commands;
using SmsScheduler;
using SmsTrackingModels;

namespace SmsSchedulerTests
{
    [TestFixture]
    public class ScheduleStatusHandlerTestFixture
    {
        public IDocumentStore DocumentStore { get; set; }

        public ScheduleStatusHandlerTestFixture()
        {
            DocumentStore = new EmbeddableDocumentStore { RunInMemory = true };
            DocumentStore.Initialize();
        }

        [Test]
        public void ScheduleSms()
        {   
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var database = "database";
            ravenDocStore.Expect(r => r.Database()).Return(database);
            ravenDocStore.Expect(r => r.GetStore().OpenSession(database)).Return(DocumentStore.OpenSession());
            var trackingData = new ScheduleTrackingData 
            {
                ScheduleId = Guid.NewGuid(), 
                MessageStatus = MessageStatus.WaitingForScheduling,
            };
            StoreDocument(trackingData, trackingData.ScheduleId.ToString());

            var scheduleStatusHandlers = new ScheduleStatusHandlers { RavenDocStore = ravenDocStore };
            var scheduleTimeUtc = DateTime.UtcNow.AddMinutes(14);
            scheduleStatusHandlers.Handle(new ScheduleStatusChanged
                {
                    ScheduleId = trackingData.ScheduleId, 
                    Status = MessageStatus.Scheduled,
                    ScheduleTimeUtc = scheduleTimeUtc
                });

            var updatedData = GetSchedule(trackingData.ScheduleId.ToString());
            Assert.That(updatedData.MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            Assert.That(updatedData.ScheduleTimeUtc, Is.EqualTo(scheduleTimeUtc));
        }

        [Test]
        public void PauseSchedule()
        {   
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var database = "database";
            ravenDocStore.Expect(r => r.Database()).Return(database);
            ravenDocStore.Expect(r => r.GetStore().OpenSession(database)).Return(DocumentStore.OpenSession());
            var trackingData = new ScheduleTrackingData 
            {
                ScheduleId = Guid.NewGuid(), 
                MessageStatus = MessageStatus.Scheduled,
            };
            StoreDocument(trackingData, trackingData.ScheduleId.ToString());

            var scheduleStatusHandlers = new ScheduleStatusHandlers { RavenDocStore = ravenDocStore };
            scheduleStatusHandlers.Handle(new ScheduleStatusChanged
                {
                    ScheduleId = trackingData.ScheduleId, 
                    Status = MessageStatus.Paused
                });

            var updatedData = GetSchedule(trackingData.ScheduleId.ToString());
            Assert.That(updatedData.MessageStatus, Is.EqualTo(MessageStatus.Paused));
        }

        [Test]
        public void SmsSent()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var database = "database";
            ravenDocStore.Expect(r => r.Database()).Return(database);
            ravenDocStore.Expect(r => r.GetStore().OpenSession(database)).Return(DocumentStore.OpenSession());
            var trackingData = new ScheduleTrackingData
            {
                ScheduleId = Guid.NewGuid(),
                MessageStatus = MessageStatus.WaitingForScheduling,
            };
            StoreDocument(trackingData, trackingData.ScheduleId.ToString());

            var scheduleStatusHandlers = new ScheduleStatusHandlers { RavenDocStore = ravenDocStore };
            var scheduleSucceeded = new ScheduleSucceeded
                {
                    ConfirmationData = new SmsConfirmationData("r", DateTime.UtcNow, 33m), ScheduleId = trackingData.ScheduleId
                };
            scheduleStatusHandlers.Handle(scheduleSucceeded);

            var updatedData = GetSchedule(trackingData.ScheduleId.ToString());
            Assert.That(updatedData.MessageStatus, Is.EqualTo(MessageStatus.Sent));
            Assert.That(updatedData.ConfirmationData, Is.EqualTo(scheduleSucceeded.ConfirmationData));
        }

        [Test]
        public void SmsFailed()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var database = "database";
            ravenDocStore.Expect(r => r.Database()).Return(database);
            ravenDocStore.Expect(r => r.GetStore().OpenSession(database)).Return(DocumentStore.OpenSession());
            var trackingData = new ScheduleTrackingData
            {
                ScheduleId = Guid.NewGuid(),
                MessageStatus = MessageStatus.WaitingForScheduling,
            };
            StoreDocument(trackingData, trackingData.ScheduleId.ToString());

            var scheduleStatusHandlers = new ScheduleStatusHandlers { RavenDocStore = ravenDocStore };
            var scheduleFailed = new ScheduleFailed
                {
                    ScheduleId = trackingData.ScheduleId,
                    Message = "fail", 
                    MoreInfo = "link"
                };
            scheduleStatusHandlers.Handle(scheduleFailed);

            var updatedData = GetSchedule(trackingData.ScheduleId.ToString());
            Assert.That(updatedData.MessageStatus, Is.EqualTo(MessageStatus.Failed));
            Assert.That(updatedData.SmsFailureData.Message, Is.EqualTo(scheduleFailed.Message));
            Assert.That(updatedData.SmsFailureData.MoreInfo, Is.EqualTo(scheduleFailed.MoreInfo));
            Assert.That(updatedData.SmsFailureData.Status, Is.EqualTo(scheduleFailed.Status));
            Assert.That(updatedData.SmsFailureData.Code, Is.EqualTo(scheduleFailed.Code));
        }

        [Test]
        public void SmsCancelled()
        {
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var database = "database";
            ravenDocStore.Expect(r => r.Database()).Return(database);
            ravenDocStore.Expect(r => r.GetStore().OpenSession(database)).Return(DocumentStore.OpenSession());
            var trackingData = new ScheduleTrackingData
            {
                ScheduleId = Guid.NewGuid(),
                MessageStatus = MessageStatus.WaitingForScheduling,
            };
            StoreDocument(trackingData, trackingData.ScheduleId.ToString());

            var scheduleStatusHandlers = new ScheduleStatusHandlers { RavenDocStore = ravenDocStore };
            scheduleStatusHandlers.Handle(new ScheduleStatusChanged
                {
                    ScheduleId = trackingData.ScheduleId,
                    Status = MessageStatus.Cancelled
                });

            var updatedData = GetSchedule(trackingData.ScheduleId.ToString());
            Assert.That(updatedData.MessageStatus, Is.EqualTo(MessageStatus.Cancelled));
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
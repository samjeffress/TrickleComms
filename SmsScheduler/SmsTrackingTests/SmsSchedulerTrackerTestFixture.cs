using System;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsTracking;
using SmsTrackingMessages.Messages;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsSchedulerTrackerTestFixture : RavenTestBase
    {
        [Test]
        public void HandleMessageScheduled()
        {
            var scheduleId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);

            var tracker = new ScheduleTracker { RavenStore = ravenDocStore };
            var scheduleCreated = new ScheduleCreated {ScheduleId = scheduleId};
            tracker.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTrackingData>(scheduleId.ToString());

                Assert.That(scheduleTracking.SmsData, Is.EqualTo(scheduleCreated.SmsData));
                Assert.That(scheduleTracking.SmsMetaData, Is.EqualTo(scheduleCreated.SmsMetaData));
                Assert.That(scheduleTracking.ScheduleId, Is.EqualTo(scheduleCreated.ScheduleId));
                Assert.That(scheduleTracking.CallerId, Is.EqualTo(scheduleCreated.CallerId));
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            }
        }

        [Test]
        public void HandleMessagePaused()
        {
            var scheduleId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var tracker = new ScheduleTracker { RavenStore = ravenDocStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTrackingData { ScheduleId = scheduleId, MessageStatus = MessageStatus.Scheduled }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleCreated = new SchedulePaused { ScheduleId = scheduleId };
            tracker.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTrackingData>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Paused));
            }
        }

        [Test]
        public void HandleMessageResumed()
        {
            var scheduleId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var tracker = new ScheduleTracker { RavenStore = ravenDocStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTrackingData { ScheduleId = scheduleId, MessageStatus = MessageStatus.Paused }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleCreated = new ScheduleResumed { ScheduleId = scheduleId };
            tracker.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTrackingData>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            }
        }

        [Test]
        public void HandleMessageCancelled()
        {
            var scheduleId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var tracker = new ScheduleTracker { RavenStore = ravenDocStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTrackingData { ScheduleId = scheduleId, MessageStatus = MessageStatus.Scheduled }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleCreated = new ScheduleCancelled { ScheduleId = scheduleId };
            tracker.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTrackingData>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Cancelled));
            }
        }

        [Test]
        public void HandleMessageSent()
        {
            var scheduleId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var tracker = new ScheduleTracker { RavenStore = ravenDocStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTrackingData { ScheduleId = scheduleId, MessageStatus = MessageStatus.Scheduled }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleComplete = new ScheduleComplete { ScheduleId = scheduleId };
            tracker.Handle(scheduleComplete);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTrackingData>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Sent));
            }
        }

        [Test]
        public void HandleMessageFailed()
        {
            var scheduleId = Guid.NewGuid();

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var tracker = new ScheduleTracker { RavenStore = ravenDocStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTrackingData { ScheduleId = scheduleId, MessageStatus = MessageStatus.Scheduled }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleFailed = new ScheduleFailed { ScheduleId = scheduleId };
            tracker.Handle(scheduleFailed);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTrackingData>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Failed));
            }
        }
    }
}
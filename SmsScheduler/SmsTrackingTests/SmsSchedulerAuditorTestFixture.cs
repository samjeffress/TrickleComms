using System;
using NUnit.Framework;
using SmsMessages.CommonData;
using SmsMessages.Scheduling;
using SmsMessages.Tracking;
using SmsTracking;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsSchedulerAuditorTestFixture : RavenTestBase
    {
        [Test]
        public void HandleMessageScheduled()
        {
            var scheduleId = Guid.NewGuid();
            var smsSentAuditor = new SmsSentAuditor { DocumentStore = DocumentStore };
            var scheduleCreated = new ScheduleCreated {ScheduleId = scheduleId};
            smsSentAuditor.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(scheduleId.ToString());

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
            var smsSentAuditor = new SmsSentAuditor { DocumentStore = DocumentStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTracking { ScheduleId = scheduleId, MessageStatus = MessageStatus.Scheduled }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleCreated = new SchedulePaused { ScheduleId = scheduleId };
            smsSentAuditor.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Paused));
            }
        }

        [Test]
        public void HandleMessageResumed()
        {
            var scheduleId = Guid.NewGuid();
            var smsSentAuditor = new SmsSentAuditor { DocumentStore = DocumentStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTracking { ScheduleId = scheduleId, MessageStatus = MessageStatus.Paused }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleCreated = new ScheduleResumed { ScheduleId = scheduleId };
            smsSentAuditor.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Scheduled));
            }
        }

        [Test]
        public void HandleMessageCancelled()
        {
            var scheduleId = Guid.NewGuid();
            var smsSentAuditor = new SmsSentAuditor { DocumentStore = DocumentStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTracking { ScheduleId = scheduleId, MessageStatus = MessageStatus.Scheduled }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleCreated = new ScheduleCancelled { ScheduleId = scheduleId };
            smsSentAuditor.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Cancelled));
            }
        }

        [Test]
        public void HandleMessageSent()
        {
            var scheduleId = Guid.NewGuid();
            var smsSentAuditor = new SmsSentAuditor { DocumentStore = DocumentStore };

            using (var session = DocumentStore.OpenSession())
            {
                session.Store(new ScheduleTracking { ScheduleId = scheduleId, MessageStatus = MessageStatus.Scheduled }, scheduleId.ToString());
                session.SaveChanges();
            }

            var scheduleCreated = new ScheduleComplete { ScheduleId = scheduleId };
            smsSentAuditor.Handle(scheduleCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(scheduleId.ToString());
                Assert.That(scheduleTracking.MessageStatus, Is.EqualTo(MessageStatus.Sent));
            }
        }
    }
}
using System;
using System.Collections.Generic;
using NUnit.Framework;
using SmsTracking;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsCoordinatorTestFixture : RavenTestBase
    {
        [Test]
        public void CoordinateMessagesCreated()
        {
            var coordinatorCreated = new CoordinatorCreated { CoordinatorId = Guid.NewGuid(), ScheduledMessages = new List<MessageSchedule> { 
                new MessageSchedule { Number = "04040044", ScheduledTime = DateTime.Now.AddMinutes(5)},
                new MessageSchedule { Number = "07777777", ScheduledTime = DateTime.Now.AddMinutes(10)} 
            }};

            var coordinatorTracker = new CoordinatorTracker { DocumentStore = DocumentStore };
            coordinatorTracker.Handle(coordinatorCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorCreated.CoordinatorId.ToString());
                Assert.That(coordinatorTrackingData.CurrentStatus, Is.EqualTo(CoordinatorStatusTracking.Started));
                Assert.That(coordinatorTrackingData.CoordinatorId, Is.EqualTo(coordinatorCreated.CoordinatorId));
                Assert.That(coordinatorTrackingData.MessageStatuses.Count, Is.EqualTo(2));
                Assert.That(coordinatorTrackingData.MessageStatuses[0].Number, Is.EqualTo(coordinatorCreated.ScheduledMessages[0].Number));
                Assert.That(coordinatorTrackingData.MessageStatuses[0].ScheduledSendingTime, Is.EqualTo(coordinatorCreated.ScheduledMessages[0].ScheduledTime));
                Assert.That(coordinatorTrackingData.MessageStatuses[0].Status, Is.EqualTo(MessageStatusTracking.Scheduled));
                Assert.That(coordinatorTrackingData.MessageStatuses[1].Number, Is.EqualTo(coordinatorCreated.ScheduledMessages[1].Number));
                Assert.That(coordinatorTrackingData.MessageStatuses[1].ScheduledSendingTime, Is.EqualTo(coordinatorCreated.ScheduledMessages[1].ScheduledTime));
                Assert.That(coordinatorTrackingData.MessageStatuses[1].Status, Is.EqualTo(MessageStatusTracking.Scheduled));
            }
        }
    }
}
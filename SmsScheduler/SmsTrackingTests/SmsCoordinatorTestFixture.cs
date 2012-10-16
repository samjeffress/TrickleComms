using System;
using System.Collections.Generic;
using System.Linq;
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

        [Test]
        public void CoordinateMessagesOneMessageCompleted()
        {
            var coordinatorId = Guid.NewGuid();
            const string updatedNumber = "04040044";
            using (var session = DocumentStore.OpenSession())
            {
                var message = new CoordinatorCreated { CoordinatorId = coordinatorId, ScheduledMessages = new List<MessageSchedule> { 
                new MessageSchedule { Number = updatedNumber, ScheduledTime = DateTime.Now.AddMinutes(5)},
                new MessageSchedule { Number = "07777777", ScheduledTime = DateTime.Now.AddMinutes(10)} 
                }};
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTime = s.ScheduledTime }).
                        ToList()
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorMessageSent = new CoordinatorMessageSent { CoordinatorId = coordinatorId, Number = updatedNumber, TimeSent = DateTime.Now.AddMinutes(7), Cost = 0.33m };
            var coordinatorTracker = new CoordinatorTracker { DocumentStore = DocumentStore };
            coordinatorTracker.Handle(coordinatorMessageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.Completed));
                Assert.That(updatedMessageData.ActualSentTime, Is.EqualTo(coordinatorMessageSent.TimeSent));
                Assert.That(updatedMessageData.Cost, Is.EqualTo(coordinatorMessageSent.Cost));
            }
        }
    }
}
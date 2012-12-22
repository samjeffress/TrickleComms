using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsTracking;
using SmsTrackingMessages.Messages;

namespace SmsTrackingTests
{
    [TestFixture]
    public class SmsCoordinatorTestFixture : RavenTestBase
    {
        [Test]
        public void CoordinateMessagesCreated()
        {
            var coordinatorCreated = new CoordinatorCreated
            {
                CoordinatorId = Guid.NewGuid(),
                ScheduledMessages = new List<MessageSchedule> { 
                new MessageSchedule { Number = "04040044", ScheduledTimeUtc = DateTime.Now.AddMinutes(5)},
                new MessageSchedule { Number = "07777777", ScheduledTimeUtc = DateTime.Now.AddMinutes(10)} 
            }
            };
            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(coordinatorCreated);

            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorCreated.CoordinatorId.ToString());
                Assert.That(coordinatorTrackingData.CurrentStatus, Is.EqualTo(CoordinatorStatusTracking.Started));
                Assert.That(coordinatorTrackingData.CoordinatorId, Is.EqualTo(coordinatorCreated.CoordinatorId));
                Assert.That(coordinatorTrackingData.MessageStatuses.Count, Is.EqualTo(2));
                Assert.That(coordinatorTrackingData.MessageStatuses[0].Number, Is.EqualTo(coordinatorCreated.ScheduledMessages[0].Number));
                Assert.That(coordinatorTrackingData.MessageStatuses[0].ScheduledSendingTimeUtc, Is.EqualTo(coordinatorCreated.ScheduledMessages[0].ScheduledTimeUtc));
                Assert.That(coordinatorTrackingData.MessageStatuses[0].Status, Is.EqualTo(MessageStatusTracking.WaitingForScheduling));
                Assert.That(coordinatorTrackingData.MessageStatuses[1].Number, Is.EqualTo(coordinatorCreated.ScheduledMessages[1].Number));
                Assert.That(coordinatorTrackingData.MessageStatuses[1].ScheduledSendingTimeUtc, Is.EqualTo(coordinatorCreated.ScheduledMessages[1].ScheduledTimeUtc));
                Assert.That(coordinatorTrackingData.MessageStatuses[1].Status, Is.EqualTo(MessageStatusTracking.WaitingForScheduling));
            }
        }

        [Test]
        public void CoordinateMessagesOneMessageScheduled()
        {
            var coordinatorId = Guid.NewGuid();
            const string updatedNumber = "04040044";
            using (var session = DocumentStore.OpenSession())
            {
                var message = new CoordinatorCreated
                {
                    CoordinatorId = coordinatorId,
                    ScheduledMessages = new List<MessageSchedule> { 
                new MessageSchedule { Number = updatedNumber, ScheduledTimeUtc = DateTime.Now.AddMinutes(5)},
                new MessageSchedule { Number = "07777777", ScheduledTimeUtc = DateTime.Now.AddMinutes(10)} 
                }
                };
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTimeUtc = s.ScheduledTimeUtc }).
                        ToList()
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorMessageSent = new CoordinatorMessageScheduled { CoordinatorId = coordinatorId, Number = updatedNumber };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(coordinatorMessageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.Scheduled));
                Assert.That(updatedMessageData.ActualSentTimeUtc, Is.Null);
                Assert.That(updatedMessageData.Cost, Is.Null);
            }
        }

        [Test]
        public void CoordinateMessagesOneMessageCompleted()
        {
            var coordinatorId = Guid.NewGuid();
            const string updatedNumber = "04040044";
            using (var session = DocumentStore.OpenSession())
            {
                var message = new CoordinatorCreated
                {
                    CoordinatorId = coordinatorId,
                    ScheduledMessages = new List<MessageSchedule> { 
                new MessageSchedule { Number = updatedNumber, ScheduledTimeUtc = DateTime.Now.AddMinutes(5)},
                new MessageSchedule { Number = "07777777", ScheduledTimeUtc = DateTime.Now.AddMinutes(10)} 
                }
                };
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTimeUtc = s.ScheduledTimeUtc }).
                        ToList()
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorMessageSent = new CoordinatorMessageSent { CoordinatorId = coordinatorId, Number = updatedNumber, TimeSentUtc = DateTime.Now.AddMinutes(7), Cost = 0.33m };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(coordinatorMessageSent);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.CompletedSuccess));
                Assert.That(updatedMessageData.ActualSentTimeUtc, Is.EqualTo(coordinatorMessageSent.TimeSentUtc));
                Assert.That(updatedMessageData.Cost, Is.EqualTo(coordinatorMessageSent.Cost));
            }
        }

        [Test]
        public void CoordinateMessagesOneMessageFailed()
        {
            var coordinatorId = Guid.NewGuid();
            const string updatedNumber = "04040044";
            using (var session = DocumentStore.OpenSession())
            {
                var message = new CoordinatorCreated
                {
                    CoordinatorId = coordinatorId,
                    ScheduledMessages = new List<MessageSchedule> { 
                new MessageSchedule { Number = updatedNumber, ScheduledTimeUtc = DateTime.Now.AddMinutes(5)},
                new MessageSchedule { Number = "07777777", ScheduledTimeUtc = DateTime.Now.AddMinutes(10)} 
                }
                };
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTimeUtc = s.ScheduledTimeUtc }).
                        ToList()
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorMessageFailed = new CoordinatorMessageFailed { CoordinatorId = coordinatorId, Number = updatedNumber, SmsFailureData = new SmsFailed(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(coordinatorMessageFailed);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.CompletedFailure));
                Assert.That(updatedMessageData.ActualSentTimeUtc, Is.Null);
                Assert.That(updatedMessageData.Cost, Is.Null);
                Assert.That(updatedMessageData.FailureData.Message, Is.EqualTo(coordinatorMessageFailed.SmsFailureData.Message));
                Assert.That(updatedMessageData.FailureData.MoreInfo, Is.EqualTo(coordinatorMessageFailed.SmsFailureData.MoreInfo));
            }
        }

        [Test]
        public void CoordinateMessagesOneMessagePausedCurrentStatusScheduled()
        {
            var coordinatorId = Guid.NewGuid();
            const string updatedNumber = "04040044";
            using (var session = DocumentStore.OpenSession())
            {
                var message = new CoordinatorCreated
                {
                    CoordinatorId = coordinatorId,
                    ScheduledMessages = new List<MessageSchedule> { 
                new MessageSchedule { Number = updatedNumber, ScheduledTimeUtc = DateTime.Now.AddMinutes(5)},
                new MessageSchedule { Number = "07777777", ScheduledTimeUtc = DateTime.Now.AddMinutes(10)} 
                }
                };
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTimeUtc = s.ScheduledTimeUtc }).
                        ToList()
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorMessagePaused = new CoordinatorMessagePaused { CoordinatorId = coordinatorId, Number = updatedNumber };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(coordinatorMessagePaused);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.Paused));
                Assert.That(updatedMessageData.ActualSentTimeUtc, Is.Null);
                Assert.That(updatedMessageData.Cost, Is.Null);
            }
        }

        [Test]
        public void CoordinateMessagesOneMessageResumedCurrentStatusPaused()
        {
            var coordinatorId = Guid.NewGuid();
            var scheduleId = Guid.NewGuid();
            const string updatedNumber = "04040044";
            var scheduledTime = DateTime.Now.AddMinutes(5);
            var rescheduledTime = DateTime.Now.AddMinutes(5);

            using (var session = DocumentStore.OpenSession())
            {
                var message = new CoordinatorCreated
                {
                    CoordinatorId = coordinatorId,
                    ScheduledMessages = new List<MessageSchedule> 
                    { 
                        new MessageSchedule { ScheduleMessageId = scheduleId, Number = updatedNumber, ScheduledTimeUtc = scheduledTime},
                        new MessageSchedule { Number = "07777777", ScheduledTimeUtc = DateTime.Now.AddMinutes(10)} 
                    }
                 };
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTimeUtc = s.ScheduledTimeUtc, Status = MessageStatusTracking.Paused, ScheduleMessageId = s.ScheduleMessageId}).
                        ToList()
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorMessageResumed = new CoordinatorMessageResumed { CoordinatorId = coordinatorId, ScheduleMessageId = scheduleId, Number = updatedNumber, RescheduledTimeUtc = rescheduledTime };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(coordinatorMessageResumed);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.Scheduled));
                Assert.That(updatedMessageData.ScheduledSendingTimeUtc, Is.EqualTo(rescheduledTime));
                Assert.That(updatedMessageData.ActualSentTimeUtc, Is.Null);
                Assert.That(updatedMessageData.Cost, Is.Null);
            }
        }

        [Test]
        public void CoordinateMessagesOneMessagePausedCurrentStatusSentThrowsException()
        {
            var coordinatorId = Guid.NewGuid();
            const string updatedNumber = "04040044";
            using (var session = DocumentStore.OpenSession())
            {
                var message = new CoordinatorCreated
                {
                    CoordinatorId = coordinatorId,
                    ScheduledMessages = new List<MessageSchedule> { 
                new MessageSchedule { Number = updatedNumber, ScheduledTimeUtc = DateTime.Now.AddMinutes(5)},
                new MessageSchedule { Number = "07777777", ScheduledTimeUtc = DateTime.Now.AddMinutes(10)} 
                }
                };
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTimeUtc = s.ScheduledTimeUtc, Status = MessageStatusTracking.CompletedSuccess }).
                        ToList()
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorMessagePaused = new CoordinatorMessagePaused { CoordinatorId = coordinatorId, Number = updatedNumber };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            Assert.That(() => coordinatorTracker.Handle(coordinatorMessagePaused), Throws.Exception.With.Message.EqualTo("Cannot record pausing of message - it is already recorded as complete."));
        }

        [Test]
        public void CoordinateMessagesCompleteWithAllMessagesComplete()
        {
            var coordinatorId = Guid.NewGuid();

            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = coordinatorId,
                    MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Number = "2323", ScheduledSendingTimeUtc = DateTime.Now, ActualSentTimeUtc = DateTime.Now, Cost = 0.33m, Status = MessageStatusTracking.Paused } }
                };
                session.Store(coordinatorTrackingData, coordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorCompleted = new CoordinatorCompleted { CoordinatorId = coordinatorId };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            Assert.That(() => coordinatorTracker.Handle(coordinatorCompleted), Throws.Exception.With.Message.EqualTo("Cannot complete coordinator - some messages are not yet complete."));
        }

        [Test]
        public void CoordinateMessagesCompleteWithSomeIncompleteMessagesThrowsException()
        {
            var coordinatorId = Guid.NewGuid();

            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = coordinatorId,
                    MessageStatuses = new List<MessageSendingStatus> { new MessageSendingStatus { Number = "2323", ScheduledSendingTimeUtc = DateTime.Now, ActualSentTimeUtc = DateTime.Now, Cost = 0.33m, Status = MessageStatusTracking.CompletedSuccess } }
                };
                session.Store(coordinatorTrackingData, coordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorCompleted = new CoordinatorCompleted { CoordinatorId = coordinatorId };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(coordinatorCompleted);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                Assert.That(trackingData.CurrentStatus, Is.EqualTo(CoordinatorStatusTracking.Completed));
            }
        }
    }
}
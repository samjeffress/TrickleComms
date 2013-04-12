using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using NUnit.Framework;
using Rhino.Mocks;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Events;
using SmsMessages.Scheduling.Events;
using SmsTracking;
using SmsTrackingMessages.Messages;
using SmsTrackingModels;

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
                ScheduledMessages = new List<MessageSchedule> 
                { 
                    new MessageSchedule { Number = "04040044", ScheduledTimeUtc = DateTime.Now.AddMinutes(5)},
                    new MessageSchedule { Number = "07777777", ScheduledTimeUtc = DateTime.Now.AddMinutes(10)} 
                },
                ConfirmationEmailAddresses = new List<string> {"tony", "barry"}
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
                Assert.That(coordinatorTrackingData.ConfirmationEmailAddress, Is.EqualTo("tony, barry"));
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

            var smsScheduled = new SmsScheduled { CoordinatorId = coordinatorId, ScheduleSendingTimeUtc = DateTime.UtcNow };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new ScheduleTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(smsScheduled);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.Scheduled));
                Assert.That(updatedMessageData.ScheduledSendingTimeUtc, Is.EqualTo(smsScheduled.ScheduleSendingTimeUtc));
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

            var scheduledSmsSent = new ScheduledSmsSent { CoordinatorId = coordinatorId, Number = updatedNumber, ConfirmationData = new SmsConfirmationData("receipt", DateTime.Now.AddMinutes(7), 0.33m) };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new ScheduleTracker() { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(scheduledSmsSent);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.CompletedSuccess));
                Assert.That(updatedMessageData.ActualSentTimeUtc, Is.EqualTo(scheduledSmsSent.ConfirmationData.SentAtUtc));
                Assert.That(updatedMessageData.Cost, Is.EqualTo(scheduledSmsSent.ConfirmationData.Price));
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

            var messageFailed = new ScheduledSmsFailed { CoordinatorId = coordinatorId, Number = updatedNumber, SmsFailedData = new SmsFailed(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty) };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new ScheduleTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(messageFailed);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                var updatedMessageData = trackingData.MessageStatuses.First(m => m.Number == updatedNumber);
                Assert.That(updatedMessageData.Status, Is.EqualTo(MessageStatusTracking.CompletedFailure));
                Assert.That(updatedMessageData.ActualSentTimeUtc, Is.Null);
                Assert.That(updatedMessageData.Cost, Is.Null);
                Assert.That(updatedMessageData.FailureData.Message, Is.EqualTo(messageFailed.SmsFailedData.Message));
                Assert.That(updatedMessageData.FailureData.MoreInfo, Is.EqualTo(messageFailed.SmsFailedData.MoreInfo));
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

            var messagePaused = new MessageSchedulePaused { CoordinatorId = coordinatorId };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new ScheduleTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(messagePaused);

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

            var messageResumed = new MessageRescheduled { CoordinatorId = coordinatorId, ScheduleMessageId = scheduleId, RescheduledTimeUtc = rescheduledTime };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new ScheduleTracker { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(messageResumed);

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

            var messagePaused = new MessageSchedulePaused { CoordinatorId = coordinatorId };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            var coordinatorTracker = new ScheduleTracker { RavenStore = ravenDocStore };
            Assert.That(() => coordinatorTracker.Handle(messagePaused), Throws.Exception.With.Message.EqualTo("Cannot record pausing of message - it is already recorded as complete."));
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
        public void CoordinateMessagesCompleteWithAllMessagesComplete()
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
            var coordinatorTracker = new CoordinatorTracker() { RavenStore = ravenDocStore };
            coordinatorTracker.Handle(coordinatorCompleted);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                Assert.That(trackingData.CurrentStatus, Is.EqualTo(CoordinatorStatusTracking.Completed));
            }
        }

        [Test]
        public void CoordinateMessagesCompleteWithAllMessagesCompleteAndSendsConfirmationEmail()
        {
            var coordinatorId = Guid.NewGuid();

            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = coordinatorId,
                    MessageStatuses = new List<MessageSendingStatus>
                        {
                            new MessageSendingStatus { Number = "2323", ScheduledSendingTimeUtc = DateTime.Now, ActualSentTimeUtc = DateTime.Now, Cost = 0.33m, Status = MessageStatusTracking.CompletedSuccess },
                            new MessageSendingStatus { Number = "2324", ScheduledSendingTimeUtc = DateTime.Now, Status = MessageStatusTracking.CompletedFailure, FailureData = new FailureData() }
                        },
                    ConfirmationEmailAddress = "email"
                };
                session.Store(coordinatorTrackingData, coordinatorId.ToString());
                session.SaveChanges();
            }

            var coordinatorCompleted = new CoordinatorCompleted { CoordinatorId = coordinatorId };

            var ravenDocStore = MockRepository.GenerateMock<IRavenDocStore>();
            var bus = MockRepository.GenerateMock<IBus>();
            ravenDocStore.Expect(r => r.GetStore()).Return(DocumentStore);
            CoordinatorCompleteEmail message;
            bus.Expect(b => b.Send(Arg<CoordinatorCompleteEmail>.Is.Anything)).WhenCalled(a => message = (CoordinatorCompleteEmail)(((Object[])(a.Arguments[0]))[0])); ;

            var coordinatorTracker = new CoordinatorTracker { RavenStore = ravenDocStore, Bus = bus};
            coordinatorTracker.Handle(coordinatorCompleted);

            using (var session = DocumentStore.OpenSession())
            {
                var trackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                Assert.That(trackingData.CurrentStatus, Is.EqualTo(CoordinatorStatusTracking.Completed));
            }
            bus.VerifyAllExpectations();
        }
    }
}
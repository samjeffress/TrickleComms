using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using Raven.Client;
using SmsMessages.Tracking;

namespace SmsTracking
{
    public class CoordinatorTracker :
        IHandleMessages<CoordinatorCreated>,
        IHandleMessages<CoordinatorMessageScheduled>,
        IHandleMessages<CoordinatorMessagePaused>,
        IHandleMessages<CoordinatorMessageSent>,
        IHandleMessages<CoordinatorCompleted>
    {
        public IDocumentStore DocumentStore { get; set; }

        public void Handle(CoordinatorCreated message)
        {
            using (var session = DocumentStore.OpenSession())
            {
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
        }

        public void Handle(CoordinatorMessageSent coordinatorMessageSent)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageSent.CoordinatorId.ToString());
                var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.Number == coordinatorMessageSent.Number);
                messageSendingStatus.ActualSentTime = coordinatorMessageSent.TimeSent;
                messageSendingStatus.Cost = coordinatorMessageSent.Cost;
                messageSendingStatus.Status = MessageStatusTracking.Completed;
                session.SaveChanges();
            }
        }

        public void Handle(CoordinatorMessagePaused coordinatorMessagePaused)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessagePaused.CoordinatorId.ToString());
                var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.Number == coordinatorMessagePaused.Number);
                if (messageSendingStatus.Status == MessageStatusTracking.Completed)
                    throw new Exception("Cannot record pausing of message - it is already recorded as complete.");
                messageSendingStatus.Status = MessageStatusTracking.Paused;
                session.SaveChanges();
            }
        }

        public void Handle(CoordinatorCompleted coordinatorCompleted)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorCompleted.CoordinatorId.ToString());
                var incompleteMessageCount = coordinatorTrackingData.MessageStatuses.Count(m => m.Status == MessageStatusTracking.Paused || m.Status == MessageStatusTracking.Scheduled);
                if (incompleteMessageCount > 0)
                    throw new Exception("Cannot complete coordinator - some messages are not yet complete.");
                coordinatorTrackingData.CurrentStatus = CoordinatorStatusTracking.Completed;
                coordinatorTrackingData.CompletionDate = coordinatorCompleted.CompletionDate;
                session.SaveChanges();
            }
        }

        public void Handle(CoordinatorMessageScheduled coordinatorMessageScheduled)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageScheduled.CoordinatorId.ToString());
                var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.Number == coordinatorMessageScheduled.Number);
                if (messageSendingStatus.Status != MessageStatusTracking.WaitingForScheduling)
                    throw new Exception("Cannot record pausing of message - it is already recorded as complete.");
                messageSendingStatus.Status = MessageStatusTracking.Scheduled;
                session.SaveChanges();
            }
        }
    }

    public class CoordinatorTrackingData
    {
        public Guid CoordinatorId { get; set; }

        public CoordinatorStatusTracking CurrentStatus { get; set; }

        public List<MessageSendingStatus> MessageStatuses { get; set; }

        public DateTime? CompletionDate { get; set; }
    }

    public class MessageSendingStatus
    {
        public string Number { get; set; }

        public DateTime ScheduledSendingTime { get; set; }

        public MessageStatusTracking Status { get; set; }

        public Decimal? Cost { get; set; }

        public DateTime? ActualSentTime { get; set; }
    }

    public enum MessageStatusTracking
    {
        WaitingForScheduling,
        Scheduled,
        Paused,
        Completed
    }

    public enum CoordinatorStatusTracking
    {
        Started,
        Paused,
        Completed
    }
}
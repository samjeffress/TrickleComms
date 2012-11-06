using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using SmsTrackingMessages;

namespace SmsTracking
{
    public class CoordinatorTracker :
        IHandleMessages<CoordinatorCreated>,
        IHandleMessages<CoordinatorMessageScheduled>,
        IHandleMessages<CoordinatorMessagePaused>,
        IHandleMessages<CoordinatorMessageResumed>,
        IHandleMessages<CoordinatorMessageSent>,
        IHandleMessages<CoordinatorCompleted>
    {
        public IRavenDocStore RavenStore { get; set; }

        public void Handle(CoordinatorCreated message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTime = s.ScheduledTime, ScheduleMessageId = s.ScheduleMessageId }).
                        ToList()
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }
        }

        public void Handle(CoordinatorMessageSent coordinatorMessageSent)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageSent.CoordinatorId.ToString());
                var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessageSent.ScheduleMessageId);
                messageSendingStatus.ActualSentTime = coordinatorMessageSent.TimeSent;
                messageSendingStatus.Cost = coordinatorMessageSent.Cost;
                messageSendingStatus.Status = MessageStatusTracking.Completed;
                session.SaveChanges();
            }
        }

        public void Handle(CoordinatorMessagePaused coordinatorMessagePaused)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessagePaused.CoordinatorId.ToString());
                var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessagePaused.ScheduleMessageId);
                if (messageSendingStatus.Status == MessageStatusTracking.Completed)
                    throw new Exception("Cannot record pausing of message - it is already recorded as complete.");
                messageSendingStatus.Status = MessageStatusTracking.Paused;
                session.SaveChanges();
            }
        }

        public void Handle(CoordinatorCompleted coordinatorCompleted)
        {
            using (var session = RavenStore.GetStore().OpenSession())
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
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageScheduled.CoordinatorId.ToString());
                var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessageScheduled.ScheduleMessageId);
                if (messageSendingStatus.Status != MessageStatusTracking.WaitingForScheduling)
                    throw new Exception("Cannot record pausing of message - it is already recorded as complete.");
                messageSendingStatus.Status = MessageStatusTracking.Scheduled;
                session.SaveChanges();
            }
        }

        public void Handle(CoordinatorMessageResumed coordinatorMessageResumed)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageResumed.CoordinatorId.ToString());
                var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessageResumed.ScheduleMessageId);
                if (messageSendingStatus.Status != MessageStatusTracking.Paused)
                    throw new Exception("Cannot record resumption of message - it is no longer paused.");
                messageSendingStatus.Status = MessageStatusTracking.Scheduled;
                messageSendingStatus.ScheduledSendingTime = coordinatorMessageResumed.RescheduledTime;
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
        public Guid ScheduleMessageId { get; set; }
        
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
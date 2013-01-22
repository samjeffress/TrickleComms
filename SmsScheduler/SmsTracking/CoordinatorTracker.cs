using System;
using System.Linq;
using NServiceBus;
using SmsTrackingMessages.Messages;
using SmsTrackingModels;

namespace SmsTracking
{
    public class CoordinatorTracker :
        IHandleMessages<CoordinatorCreated>,
        //IHandleMessages<CoordinatorMessageScheduled>,
        //IHandleMessages<CoordinatorMessagePaused>,
        //IHandleMessages<CoordinatorMessageResumed>,
        //IHandleMessages<CoordinatorMessageSent>,
        //IHandleMessages<CoordinatorMessageFailed>,
        IHandleMessages<CoordinatorCompleted>
    {
        public IRavenDocStore RavenStore { get; set; }

        public IBus Bus { get; set; }

        public void Handle(CoordinatorCreated message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    MessageStatuses = message.ScheduledMessages
                        .Select(s => new MessageSendingStatus { Number = s.Number, ScheduledSendingTimeUtc = s.ScheduledTimeUtc, ScheduleMessageId = s.ScheduleMessageId }).
                        ToList(),
                    CreationDateUtc = message.CreationDateUtc,
                    MetaData = message.MetaData,
                    ConfirmationEmailAddress = message.ConfirmationEmailAddress
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }
        }

        //public void Handle(CoordinatorMessageSent coordinatorMessageSent)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageSent.CoordinatorId.ToString());
        //        var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessageSent.ScheduleMessageId);
        //        messageSendingStatus.ActualSentTimeUtc = coordinatorMessageSent.TimeSentUtc;
        //        messageSendingStatus.Cost = coordinatorMessageSent.Cost;
        //        messageSendingStatus.Status = MessageStatusTracking.CompletedSuccess;
        //        session.SaveChanges();
        //    }
        //}

        //public void Handle(CoordinatorMessagePaused coordinatorMessagePaused)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessagePaused.CoordinatorId.ToString());
        //        var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessagePaused.ScheduleMessageId);
        //        if (messageSendingStatus.Status == MessageStatusTracking.CompletedSuccess)
        //            throw new Exception("Cannot record pausing of message - it is already recorded as complete.");
        //        messageSendingStatus.Status = MessageStatusTracking.Paused;
        //        session.SaveChanges();
        //    }
        //}

        public void Handle(CoordinatorCompleted coordinatorCompleted)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorCompleted.CoordinatorId.ToString());
                var incompleteMessageCount = coordinatorTrackingData.MessageStatuses.Count(m => m.Status == MessageStatusTracking.Paused || m.Status == MessageStatusTracking.Scheduled);
                if (incompleteMessageCount > 0)
                    throw new Exception("Cannot complete coordinator - some messages are not yet complete.");
                coordinatorTrackingData.CurrentStatus = CoordinatorStatusTracking.Completed;
                coordinatorTrackingData.CompletionDateUtc = coordinatorCompleted.CompletionDate;
                if (!string.IsNullOrWhiteSpace(coordinatorTrackingData.ConfirmationEmailAddress))
                {
                    var coordinatorCompleteEmail = new CoordinatorCompleteEmail();
                    coordinatorCompleteEmail.CoordinatorId = coordinatorTrackingData.CoordinatorId;
                    coordinatorCompleteEmail.EmailAddress = coordinatorTrackingData.ConfirmationEmailAddress;
                    coordinatorCompleteEmail.FinishTimeUtc = coordinatorTrackingData.CompletionDateUtc.Value;
                    coordinatorCompleteEmail.StartTimeUtc = coordinatorTrackingData.CreationDateUtc;
                    coordinatorCompleteEmail.SendingData = new SendingData
                    {
                        SuccessfulMessages = coordinatorTrackingData.MessageStatuses
                            .Where(m => m.Status == MessageStatusTracking.CompletedSuccess)
                            .Select(m => new SuccessfulMessage
                            {
                                Cost = m.Cost.Value, 
                                ScheduleId = m.ScheduleMessageId, 
                                TimeSentUtc = m.ActualSentTimeUtc.Value
                            })
                            .ToList(),
                        UnsuccessfulMessageses = coordinatorTrackingData.MessageStatuses
                            .Where(m => m.Status == MessageStatusTracking.CompletedFailure)
                            .Select(m => new UnsuccessfulMessage
                            {
                                ScheduleId = m.ScheduleMessageId, 
                                FailureReason = new FailureReason { Message = m.FailureData.Message, MoreInfo = m.FailureData.MoreInfo }, 
                                ScheduleSendingTimeUtc = m.ScheduledSendingTimeUtc
                            })
                            .ToList(),
                    };
                    Bus.Send(coordinatorCompleteEmail);
                }
                session.SaveChanges();
            }
        }

        //public void Handle(CoordinatorMessageScheduled coordinatorMessageScheduled)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageScheduled.CoordinatorId.ToString());
        //        var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessageScheduled.ScheduleMessageId);
        //        if (messageSendingStatus.Status != MessageStatusTracking.WaitingForScheduling)
        //            throw new Exception("Cannot record pausing of message - it is already recorded as complete.");
        //        messageSendingStatus.Status = MessageStatusTracking.Scheduled;
        //        session.SaveChanges();
        //    }
        //}

        //public void Handle(CoordinatorMessageResumed coordinatorMessageResumed)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageResumed.CoordinatorId.ToString());
        //        var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessageResumed.ScheduleMessageId);
        //        if (messageSendingStatus.Status != MessageStatusTracking.Paused)
        //            throw new Exception("Cannot record resumption of message - it is no longer paused.");
        //        messageSendingStatus.Status = MessageStatusTracking.Scheduled;
        //        messageSendingStatus.ScheduledSendingTimeUtc = coordinatorMessageResumed.RescheduledTimeUtc;
        //        session.SaveChanges();
        //    }
        //}

        //public void Handle(CoordinatorMessageFailed coordinatorMessageFailed)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorMessageFailed.CoordinatorId.ToString());
        //        var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == coordinatorMessageFailed.ScheduleMessageId);
        //        messageSendingStatus.Status = MessageStatusTracking.CompletedFailure;
        //        messageSendingStatus.FailureData = new FailureData { Message = coordinatorMessageFailed.SmsFailureData.Message, MoreInfo = coordinatorMessageFailed.SmsFailureData.MoreInfo };
        //        session.SaveChanges();
        //    }
        //}
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using NServiceBus;
using SmsMessages.Coordinator.Events;
using SmsTrackingMessages.Messages;
using SmsTrackingModels;

namespace SmsTracking
{
    public class CoordinatorTracker :
        IHandleMessages<CoordinatorCompleted>
    {
        public IRavenDocStore RavenStore { get; set; }

        public IBus Bus { get; set; }

        public void Handle(CoordinatorCompleted coordinatorCompleted)
        {
            // TODO : Remove this - move to smscoordinator
            using (var session = RavenStore.GetStore().OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorCompleted.CoordinatorId.ToString());
                var coordinatorCompleteEmail = new CoordinatorCompleteEmail();
                coordinatorCompleteEmail.CoordinatorId = coordinatorTrackingData.CoordinatorId;
                coordinatorCompleteEmail.EmailAddresses = string.IsNullOrWhiteSpace(coordinatorTrackingData.ConfirmationEmailAddress) ? new List<string>() : coordinatorTrackingData.ConfirmationEmailAddress.Split(',').ToList().Select(e => e.Trim()).ToList();
                coordinatorCompleteEmail.UserOlsenTimeZone = coordinatorTrackingData.UserOlsenTimeZone;
                coordinatorCompleteEmail.FinishTimeUtc = coordinatorCompleted.CompletionDateUtc;
                coordinatorCompleteEmail.StartTimeUtc = coordinatorTrackingData.CreationDateUtc;
                coordinatorCompleteEmail.Topic = coordinatorTrackingData.MetaData.Topic;
                //coordinatorCompleteEmail.SendingData = new SendingData
                //{
                //    SuccessfulMessages = coordinatorTrackingData.MessageStatuses
                //        .Where(m => m.Status == MessageStatusTracking.CompletedSuccess)
                //        .Select(m => new SuccessfulMessage
                //        {
                //            Cost = m.Cost.Value, 
                //            ScheduleId = m.ScheduleMessageId, 
                //            TimeSentUtc = m.ActualSentTimeUtc.Value
                //        })
                //        .ToList(),
                //    UnsuccessfulMessageses = coordinatorTrackingData.MessageStatuses
                //        .Where(m => m.Status == MessageStatusTracking.CompletedFailure)
                //        .Select(m => new UnsuccessfulMessage
                //        {
                //            ScheduleId = m.ScheduleMessageId, 
                //            FailureReason = new FailureReason { Message = m.FailureData.Message, MoreInfo = m.FailureData.MoreInfo }, 
                //            ScheduleSendingTimeUtc = m.ScheduledSendingTimeUtc
                //        })
                //        .ToList(),
                //};
                Bus.Send(coordinatorCompleteEmail);
                session.SaveChanges();
            }
        }
    }
}
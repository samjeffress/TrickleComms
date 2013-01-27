using System;
using System.Linq;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Scheduling.Events;
using SmsTrackingModels;

namespace SmsTracking
{
    public class ScheduleTracker : 
        IHandleMessages<SmsScheduled>,
        IHandleMessages<ScheduledSmsSent>,
        IHandleMessages<MessageSchedulePaused>,
        IHandleMessages<MessageRescheduled>,
        IHandleMessages<ScheduledSmsFailed>
        // TODO : Add some sort of 'Cancelled Message' handler?
    {
        public IRavenDocStore RavenStore { get; set; }

        public void Handle(SmsScheduled message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                if (message.CoordinatorId == Guid.Empty)
                {
                    var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleMessageId.ToString());
                    if (scheduleTracking != null)
                        throw new ArgumentException("ScheduleTrackingData already exists with Id " + message.ScheduleMessageId.ToString());
                    var tracker = new ScheduleTrackingData
                    {
                        //CallerId = message.CallerId,
                        MessageStatus = MessageStatus.Scheduled,
                        ScheduleId = message.ScheduleMessageId,
                        SmsData = message.SmsData,
                        SmsMetaData = message.SmsMetaData,
                        ScheduleTimeUtc = message.ScheduleSendingTimeUtc
                    };
                    session.Store(tracker, message.ScheduleMessageId.ToString());
                    session.SaveChanges();
                }
                else
                {
                    var coordinator = session.Load<CoordinatorTrackingData>(message.CoordinatorId.ToString());    
                    if (coordinator == null) throw new ArgumentException("Coordinator not created yet.");
                    var messageSendingStatus = coordinator.MessageStatuses.First(m => m.ScheduleMessageId == message.ScheduleMessageId);
                    messageSendingStatus.Status = MessageStatusTracking.Scheduled;
                    messageSendingStatus.ScheduledSendingTimeUtc = message.ScheduleSendingTimeUtc;
                    session.SaveChanges();
                }
            }
        }

        public void Handle(ScheduledSmsSent message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                if (message.CoordinatorId == Guid.Empty)
                {
                    var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduledSmsId.ToString());
                    if (scheduleTracking == null)
                        throw new ArgumentException("ScheduleTrackingData could not be found with Id " + message.ScheduledSmsId.ToString());
                    scheduleTracking.MessageStatus = MessageStatus.Sent;
                    scheduleTracking.ConfirmationData = message.ConfirmationData;
                    session.SaveChanges();
                }
                else
                {
                    var coordinator = session.Load<CoordinatorTrackingData>(message.CoordinatorId.ToString());
                    if (coordinator == null) throw new ArgumentException("Coordinator not created yet.");
                    var messageSendingStatus = coordinator.MessageStatuses.First(m => m.ScheduleMessageId == message.ScheduledSmsId);
                    messageSendingStatus.Status = MessageStatusTracking.CompletedSuccess;
                    messageSendingStatus.ActualSentTimeUtc = message.ConfirmationData.SentAtUtc;
                    messageSendingStatus.Cost = message.ConfirmationData.Price;
                    session.SaveChanges();
                }
            }
        }

        public void Handle(MessageSchedulePaused message)
        {
            using (var session =  RavenStore.GetStore().OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                if(message.CoordinatorId == Guid.Empty)
                {
                    var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
                    if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
                    scheduleTracking.MessageStatus = MessageStatus.Paused;
                    session.SaveChanges();
                }
                else
                {
                    var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(message.CoordinatorId.ToString());
                    var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == message.ScheduleId);
                    if (messageSendingStatus.Status == MessageStatusTracking.CompletedSuccess)
                        throw new Exception("Cannot record pausing of message - it is already recorded as complete.");
                    messageSendingStatus.Status = MessageStatusTracking.Paused;
                    session.SaveChanges();
                }
            }
        }

        public void Handle(MessageRescheduled message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                if (message.CoordinatorId == Guid.Empty)
                {
                    var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleMessageId.ToString());
                    if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleMessageId);
                    scheduleTracking.MessageStatus = MessageStatus.Scheduled;
                    scheduleTracking.ScheduleTimeUtc = message.RescheduledTimeUtc;
                    session.SaveChanges();
                }
                else
                {
                    var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(message.CoordinatorId.ToString());
                    var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == message.ScheduleMessageId);
                    if (messageSendingStatus.Status == MessageStatusTracking.CompletedSuccess)
                        throw new Exception("Cannot record pausing of message - it is already recorded as complete.");
                    messageSendingStatus.Status = MessageStatusTracking.Scheduled;
                    messageSendingStatus.ScheduledSendingTimeUtc = message.RescheduledTimeUtc;
                    session.SaveChanges();
                }
            }
        }

        public void Handle(ScheduledSmsFailed message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
                session.Advanced.UseOptimisticConcurrency = true;
                if (message.CoordinatorId == Guid.Empty)
                {
                    var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduledSmsId.ToString());
                    if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduledSmsId);
                    scheduleTracking.MessageStatus = MessageStatus.Failed;
                    scheduleTracking.SmsFailureData = message.SmsFailedData;
                    session.SaveChanges();
                }
                else
                {
                    var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(message.CoordinatorId.ToString());
                    var messageSendingStatus = coordinatorTrackingData.MessageStatuses.First(m => m.ScheduleMessageId == message.ScheduledSmsId);
                    messageSendingStatus.Status = MessageStatusTracking.CompletedFailure;
                    messageSendingStatus.FailureData = new FailureData { Message = message.SmsFailedData.Message, MoreInfo = message.SmsFailedData.MoreInfo };
                    session.SaveChanges();
                }
            }
        }
    }
}

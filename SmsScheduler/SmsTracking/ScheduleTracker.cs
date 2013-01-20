using System;
using System.Linq;
using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Scheduling.Events;
using SmsTrackingMessages.Messages;

namespace SmsTracking
{
    public class ScheduleTracker : 
        //IHandleMessages<ScheduleCreated>,
        
    //IHandleMessages<SchedulePaused>,
    //    IHandleMessages<ScheduleResumed>,
    //    IHandleMessages<ScheduleCancelled>,
    //    IHandleMessages<ScheduleComplete>,
    //    IHandleMessages<ScheduleFailed>
        IHandleMessages<SmsScheduled>,
        IHandleMessages<ScheduledSmsSent>,
        IHandleMessages<MessageSchedulePaused>,
        IHandleMessages<MessageRescheduled>,
        IHandleMessages<ScheduledSmsFailed>
        // TODO : Add some sort of 'Cancelled Message' handler?
    {
        public IRavenDocStore RavenStore { get; set; }

        //public void Handle(ScheduleCreated message)
        //{
        //    //using (var session = RavenStore.GetStore().OpenSession())
        //    //{
        //    //    var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleId);
        //    //    if (scheduleTracking != null) return;
        //    //    var tracker = new ScheduleTrackingData
        //    //    {
        //    //        CallerId = message.CallerId,
        //    //        MessageStatus = MessageStatus.Scheduled,
        //    //        ScheduleId = message.ScheduleId,
        //    //        SmsData = message.SmsData,
        //    //        SmsMetaData = message.SmsMetaData
        //    //    };
        //    //    session.Store(tracker, message.ScheduleId.ToString());
        //    //    session.SaveChanges();
        //    //}
        //}

        public void Handle(SmsScheduled message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
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
                    var messageSendingStatus = coordinator.MessageStatuses.Where(m => m.ScheduleMessageId == message.ScheduleMessageId).First();
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

        //public void Handle(SchedulePaused message)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
        //        if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
        //        scheduleTracking.MessageStatus = MessageStatus.Paused;
        //        session.SaveChanges();
        //    }
        //}

        //public void Handle(ScheduleResumed message)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
        //        if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
        //        scheduleTracking.MessageStatus = MessageStatus.Scheduled;
        //        session.SaveChanges();
        //    }
        //}

        //public void Handle(ScheduleCancelled message)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
        //        if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
        //        scheduleTracking.MessageStatus = MessageStatus.Cancelled;
        //        session.SaveChanges();
        //    }
        //}

        //public void Handle(ScheduleComplete message)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
        //        if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
        //        scheduleTracking.MessageStatus = MessageStatus.Sent;
        //        session.SaveChanges();
        //    }
        //}

        public void Handle(MessageSchedulePaused message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
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

        //public void Handle(ScheduleFailed message)
        //{
        //    using (var session = RavenStore.GetStore().OpenSession())
        //    {
        //        var scheduleTracking = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
        //        if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
        //        scheduleTracking.MessageStatus = MessageStatus.Failed;
        //        session.SaveChanges();
        //    }
        //}

        public void Handle(MessageRescheduled message)
        {
            using (var session = RavenStore.GetStore().OpenSession())
            {
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

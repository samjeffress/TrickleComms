using System;
using NServiceBus;
using Raven.Client;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using SmsMessages.Tracking;

namespace SmsTracking
{
    public class SmsSentTracker : 
        IHandleMessages<MessageSent>,
        IHandleMessages<ScheduleCreated>,
        IHandleMessages<SchedulePaused>,
        IHandleMessages<ScheduleResumed>,
        IHandleMessages<ScheduleCancelled>,
        IHandleMessages<ScheduleComplete>
    {
        public IDocumentStore DocumentStore { get; set; }

        public void Handle(MessageSent message)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var messageSent = session.Load<MessageSent>(message.ConfirmationData.Receipt);
                if (messageSent != null) return;
                session.Store(message, message.ConfirmationData.Receipt);
                session.SaveChanges();
            }
        }

        public void Handle(ScheduleCreated message)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(message.ScheduleId);
                if (scheduleTracking != null) return;
                var tracker = new ScheduleTracking
                {
                    CallerId = message.CallerId, 
                    MessageStatus = MessageStatus.Scheduled, 
                    ScheduleId = message.ScheduleId, 
                    SmsData = message.SmsData, 
                    SmsMetaData = message.SmsMetaData
                };
                session.Store(tracker, message.ScheduleId.ToString());
                session.SaveChanges();
            }
        }

        public void Handle(SchedulePaused message)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(message.ScheduleId.ToString());
                if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
                scheduleTracking.MessageStatus = MessageStatus.Paused;
                session.SaveChanges();
            }
        }

        public void Handle(ScheduleResumed message)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(message.ScheduleId.ToString());
                if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
                scheduleTracking.MessageStatus = MessageStatus.Scheduled;
                session.SaveChanges();
            }
        }

        public void Handle(ScheduleCancelled message)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(message.ScheduleId.ToString());
                if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
                scheduleTracking.MessageStatus = MessageStatus.Cancelled;
                session.SaveChanges();
            }
        }

        public void Handle(ScheduleComplete message)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var scheduleTracking = session.Load<ScheduleTracking>(message.ScheduleId.ToString());
                if (scheduleTracking == null) throw new Exception("Could not find schedule id " + message.ScheduleId);
                scheduleTracking.MessageStatus = MessageStatus.Sent;
                session.SaveChanges();
            }
        }
    }
}

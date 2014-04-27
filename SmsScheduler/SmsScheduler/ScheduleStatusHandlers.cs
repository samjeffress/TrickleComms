using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Tracking.Scheduling.Commands;
using SmsTrackingModels;

namespace SmsScheduler
{
    public class ScheduleStatusHandlers :
        IHandleMessages<EmailScheduleCreated>,
        IHandleMessages<ScheduleCreated>,
        IHandleMessages<ScheduleStatusChanged>,
        IHandleMessages<ScheduleSucceeded>,
        IHandleMessages<ScheduleFailed>
    {
        public IRavenDocStore RavenDocStore { get; set; }

        // TODO: Save the user that created the request too
        
        public void Handle(EmailScheduleCreated message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(RavenDocStore.Database()))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
                if (scheduleTrackingData == null)
                {
                    var scheduleTracker = new ScheduleTrackingData
                    {
                        MessageStatus = MessageStatus.Scheduled,
                        ScheduleId = message.ScheduleId,
                        EmailData = message.EmailData, 
                        SmsMetaData = new SmsMetaData { Tags = message.Tags, Topic = message.Topic },
                        ScheduleTimeUtc = message.ScheduleTimeUtc
                    };
                    session.Store(scheduleTracker, message.ScheduleId.ToString());
                }
                else
                {
                    scheduleTrackingData.MessageStatus = MessageStatus.Scheduled;
                }
                session.SaveChanges();
            }
        }

        public void Handle(ScheduleCreated message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(RavenDocStore.Database()))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
                if (scheduleTrackingData == null)
                {
                    var scheduleTracker = new ScheduleTrackingData
                    {
                        MessageStatus = MessageStatus.Scheduled,
                        ScheduleId = message.ScheduleId,
                        SmsData = message.SmsData,
                        SmsMetaData = message.SmsMetaData,
                        ScheduleTimeUtc = message.ScheduleTimeUtc
                    };
                    session.Store(scheduleTracker, message.ScheduleId.ToString());
                }
                else
                {
                    scheduleTrackingData.MessageStatus = MessageStatus.Scheduled;
                }
                session.SaveChanges();
            }
        }

        public void Handle(ScheduleStatusChanged message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(RavenDocStore.Database()))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
                scheduleTrackingData.MessageStatus = message.Status;
                if (message.ScheduleTimeUtc.HasValue)
                    scheduleTrackingData.ScheduleTimeUtc = message.ScheduleTimeUtc.Value;
                session.SaveChanges();
            }
        }

        public void Handle(ScheduleSucceeded message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(RavenDocStore.Database()))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
                scheduleTrackingData.MessageStatus = MessageStatus.Sent;
                scheduleTrackingData.ConfirmationData = message.ConfirmationData;
                session.SaveChanges();
            }
        }

        public void Handle(ScheduleFailed message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(RavenDocStore.Database()))
            {
                var scheduleTrackingData = session.Load<ScheduleTrackingData>(message.ScheduleId.ToString());
                scheduleTrackingData.MessageStatus = MessageStatus.Failed;
                scheduleTrackingData.SmsFailureData = new SmsFailed("", message.Code, message.Message, message.MoreInfo, message.Status);
                session.SaveChanges();
            }
        }
    }
}
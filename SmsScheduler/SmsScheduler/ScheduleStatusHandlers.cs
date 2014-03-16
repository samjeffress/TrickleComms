using NServiceBus;
using SmsMessages.CommonData;
using SmsMessages.Tracking.Scheduling.Commands;
using SmsTrackingModels;

namespace SmsScheduler
{
    public class ScheduleStatusHandlers :
        IHandleMessages<ScheduleStatusChanged>,
        IHandleMessages<ScheduleSucceeded>,
        IHandleMessages<ScheduleFailed>
    {
        public IRavenDocStore RavenDocStore { get; set; }

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
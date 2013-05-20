using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client;
using SmsMessages.CommonData;
using SmsMessages.Scheduling.Commands;
using SmsTrackingModels;

namespace SmsCoordinator
{
    public interface IRavenScheduleDocuments
    {
        List<ScheduleTrackingData> GetPausedScheduleTrackingData(Guid coordinatorId);
        List<ScheduleTrackingData> GetActiveScheduleTrackingData(Guid coordinatorId);
        void SaveSchedules(List<ScheduleSmsForSendingLater> messageList, Guid coordinatorId);
    }

    public class RavenScheduleDocuments : IRavenScheduleDocuments
    {
        public IRavenDocStore RavenDocStore { get; set; }

        public List<ScheduleTrackingData> GetActiveScheduleTrackingData(Guid coordinatorId)
        {
            var trackingData = new List<ScheduleTrackingData>();
            int page = 0;
            int pageSize = 100;
            RavenQueryStatistics ravenStats;

            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                    .Statistics(out ravenStats)
                    .FirstOrDefault(s => s.CoordinatorId == coordinatorId &&
                                (s.MessageStatus == MessageStatus.Scheduled ||
                                 s.MessageStatus == MessageStatus.WaitingForScheduling));
            }

            while (ravenStats.TotalResults > (page) * pageSize)
            {
                using (var session = RavenDocStore.GetStore().OpenSession())
                {
                    var tracking = session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                        .Where(s => s.CoordinatorId == coordinatorId)
                        .Where(
                            s =>
                            s.MessageStatus == MessageStatus.Scheduled ||
                            s.MessageStatus == MessageStatus.WaitingForScheduling)
                        .Skip(pageSize * page)
                        .Take(pageSize).ToList();
                    trackingData.AddRange(tracking);
                }
                page++;
            }
            return trackingData;
        }

        public List<ScheduleTrackingData> GetPausedScheduleTrackingData(Guid coordinatorId)
        {
            var trackingData = new List<ScheduleTrackingData>();
            int page = 0;
            int pageSize = 100;
            RavenQueryStatistics ravenStats;

            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                    .Statistics(out ravenStats)
                    .FirstOrDefault(s => s.CoordinatorId == coordinatorId &&
                                s.MessageStatus == MessageStatus.Paused);
            }

            while (ravenStats.TotalResults > (page) * pageSize)
            {
                using (var session = RavenDocStore.GetStore().OpenSession())
                {
                    var tracking = session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                        .Where(s => s.CoordinatorId == coordinatorId)
                        .Where(s => s.MessageStatus == MessageStatus.Paused)
                        .Skip(pageSize * page)
                        .Take(pageSize).ToList();
                    trackingData.AddRange(tracking);
                }
                page++;
            }
            return trackingData;
        }

        public void SaveSchedules(List<ScheduleSmsForSendingLater> messageList, Guid coordinatorId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                foreach (var scheduleSmsForSendingLater in messageList)
                {
                    var scheduleTracker = new ScheduleTrackingData
                    {
                        MessageStatus = MessageStatus.WaitingForScheduling,
                        ScheduleId = scheduleSmsForSendingLater.ScheduleMessageId,
                        SmsData = scheduleSmsForSendingLater.SmsData,
                        SmsMetaData = scheduleSmsForSendingLater.SmsMetaData,
                        ScheduleTimeUtc = scheduleSmsForSendingLater.SendMessageAtUtc,
                        CoordinatorId = coordinatorId
                    };
                    session.Store(scheduleTracker, scheduleSmsForSendingLater.ScheduleMessageId.ToString());
                }
                session.SaveChanges();
            }
        }
    }
}
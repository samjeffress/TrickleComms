using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Events;
using SmsMessages.Scheduling.Commands;
using SmsTrackingModels;

namespace SmsCoordinator
{
    public interface IRavenScheduleDocuments
    {
        List<ScheduleTrackingData> GetActiveScheduleTrackingData(Guid coordinatorId);
        void SaveSchedules(List<ScheduleSmsForSendingLater> messageList, Guid coordinatorId);
        DateTime GetMaxScheduleDateTime(Guid coordinatorId);
        bool AreCoordinatedSchedulesComplete(Guid coordinatorId);
        void SaveCoordinator(CoordinatorCreated message);
        void MarkCoordinatorAsComplete(Guid coordinatorId, DateTime utcCompleteDate);
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
                        .OrderBy(s => s.ScheduleId)
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

        public DateTime GetMaxScheduleDateTime(Guid coordinatorId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                return session.Query<ScheduleTrackingData>()
                   .Customize(s => s.WaitForNonStaleResultsAsOfNow())
                   .Where(x => x.CoordinatorId == coordinatorId)
                   .OrderByDescending(x => x.ScheduleTimeUtc)
                   .Select(s => s.ScheduleTimeUtc)
                   .FirstOrDefault();
            }
        }

        public bool AreCoordinatedSchedulesComplete(Guid coordinatorId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var reduceResult = session
                    .Query<ScheduledMessages_ByCoordinatorIdAndStatus.ReduceResult, ScheduledMessages_ByCoordinatorIdAndStatus>()
                    .Customize(s => s.WaitForNonStaleResultsAsOfNow())
                    .Where(s => s.CoordinatorId == coordinatorId.ToString() 
                        && (
                        s.Status == MessageStatus.WaitingForScheduling.ToString() || 
                        s.Status == MessageStatus.Scheduled.ToString() || 
                        s.Status == MessageStatus.Paused.ToString()))
                    .FirstOrDefault();
                return reduceResult == null || reduceResult.Count == 0;
            }
        }

        public void SaveCoordinator(CoordinatorCreated message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    CreationDateUtc = message.CreationDateUtc,
                    MetaData = message.MetaData,
                    ConfirmationEmailAddress = String.Join(", ", message.ConfirmationEmailAddresses),
                    UserOlsenTimeZone = message.UserOlsenTimeZone,
                    CurrentStatus = CoordinatorStatusTracking.Started,
                    MessageBody = message.MessageBody,
                    MessageCount = message.MessageCount
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }
        }

        public void MarkCoordinatorAsComplete(Guid coordinatorId, DateTime utcCompleteDate)
        {
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                coordinatorTrackingData.CompletionDateUtc = utcCompleteDate;
                coordinatorTrackingData.CurrentStatus = CoordinatorStatusTracking.Completed;
                session.SaveChanges();
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Events;
using SmsMessages.Scheduling.Commands;
using SmsTrackingModels;
using SmsTrackingModels.RavenIndexs;

namespace SmsCoordinator
{
    public interface IRavenScheduleDocuments
    {
        List<ScheduleTrackingData> GetActiveScheduleTrackingData(Guid coordinatorId);
        void SaveSchedules(List<ScheduleSmsForSendingLater> messageList, Guid coordinatorId);
        void SaveSchedules(List<object> messageList, Guid coordinatorId);
        DateTime GetMaxScheduleDateTime(Guid coordinatorId);
        bool AreCoordinatedSchedulesComplete(Guid coordinatorId);
        void SaveCoordinator(CoordinatorCreated message);
        void SaveCoordinator(CoordinatorCreatedWithEmailAndSms message);
        void MarkCoordinatorAsComplete(Guid coordinatorId, DateTime utcCompleteDate);
        List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult> GetScheduleSummary(Guid coordinatorId);
        CustomerContactList GetSmsAndEmailCoordinatorData(string smsAndEmailDataId);
    }

    public class RavenScheduleDocuments : IRavenScheduleDocuments
    {
        private const string Database = "SmsTracking";
        public ILog Log { get; set; }
        public IRavenDocStore RavenDocStore { get; set; }

        public List<ScheduleTrackingData> GetActiveScheduleTrackingData(Guid coordinatorId)
        {
            var trackingData = new List<ScheduleTrackingData>();
            int page = 0;
            int pageSize = 100;
            RavenQueryStatistics ravenStats;

            using (var session = RavenDocStore.GetStore().OpenSession(Database))
            {
                session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                    .Statistics(out ravenStats)
                    .FirstOrDefault(s => s.CoordinatorId == coordinatorId &&
                                (s.MessageStatus == MessageStatus.Scheduled ||
                                 s.MessageStatus == MessageStatus.WaitingForScheduling ||
                                 s.MessageStatus == MessageStatus.Paused));
            }

            while (ravenStats.TotalResults > (page) * pageSize)
            {
                using (var session = RavenDocStore.GetStore().OpenSession(Database))
                {
                    var tracking = session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                        .Where(s => s.CoordinatorId == coordinatorId)
                        .Where(
                            s =>
                            s.MessageStatus == MessageStatus.Scheduled ||
                            s.MessageStatus == MessageStatus.WaitingForScheduling ||
                            s.MessageStatus == MessageStatus.Paused)
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
            var scheduleSummary = GetScheduleSummary(coordinatorId);
            var scheduleCount = scheduleSummary.Sum(s => s.Count);

            if (scheduleCount > 0 && scheduleCount == messageList.Count)
            {
                return;
            }
            // else - delete the existing schedules????
            using (var session = RavenDocStore.GetStore().BulkInsert(Database))
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

        public void SaveSchedules(List<object> messageList, Guid coordinatorId)
        {
            var scheduleSummary = GetScheduleSummary(coordinatorId);
            var scheduleCount = scheduleSummary.Sum(s => s.Count);

            if (scheduleCount > 0 && scheduleCount == messageList.Count)
            {
                return;
            }
            // else - delete the existing schedules????
            using (var session = RavenDocStore.GetStore().BulkInsert(Database))
            {
                // TODO: Check if count of documents with coordinator id match number of schedules (in case they've already been saved)
                foreach (var schedule in messageList)
                {
                    if (schedule.GetType() == typeof (ScheduleSmsForSendingLater))
                    {
                        var smsSchedule = schedule as ScheduleSmsForSendingLater;
                        var scheduleTracker = new ScheduleTrackingData
                        {
                            MessageStatus = MessageStatus.WaitingForScheduling,
                            ScheduleId = smsSchedule.ScheduleMessageId,
                            SmsData = smsSchedule.SmsData,
                            SmsMetaData = smsSchedule.SmsMetaData,
                            ScheduleTimeUtc = smsSchedule.SendMessageAtUtc,
                            CoordinatorId = coordinatorId,
                            Username = smsSchedule.Username
                        };
                        session.Store(scheduleTracker, smsSchedule.ScheduleMessageId.ToString());   
                    }
                    if (schedule.GetType() == typeof (ScheduleEmailForSendingLater))
                    {
                        var emailSchedule = schedule as ScheduleEmailForSendingLater;

                        var scheduleTracker = new ScheduleTrackingData
                        {
                            MessageStatus = MessageStatus.WaitingForScheduling,
                            ScheduleId = emailSchedule.ScheduleMessageId,
                            EmailData = emailSchedule.EmailData,
                            SmsMetaData = new SmsMetaData { Tags = emailSchedule.Tags, Topic = emailSchedule.Topic },
                            ScheduleTimeUtc = emailSchedule.SendMessageAtUtc,
                            CoordinatorId = coordinatorId,
                            Username = emailSchedule.Username
                        };
                        session.Store(scheduleTracker, emailSchedule.ScheduleMessageId.ToString());  
                    }

                }
                //session.SaveChanges();
            }
        }

        public DateTime GetMaxScheduleDateTime(Guid coordinatorId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(Database))
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
            using (var session = RavenDocStore.GetStore().OpenSession(Database))
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
            Log.Error("Saving coordinator data");
            bool trackingDataExists;
            using (var session = RavenDocStore.GetStore().OpenSession(Database))
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(message.CoordinatorId.ToString());
                trackingDataExists = coordinatorTrackingData != null;
            }

            if (trackingDataExists) return;
            using (var session = RavenDocStore.GetStore().BulkInsert(Database))
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
                        MessageCount = message.MessageCount,
                        Username = message.UserName
                    };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                session.SaveChanges();
            }
        }

        public void SaveCoordinator(CoordinatorCreatedWithEmailAndSms message)
        {
            throw new NotImplementedException("Probably need to store a separate model");
            bool trackingDataExists;
            using (var session = RavenDocStore.GetStore().OpenSession(Database))
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(message.CoordinatorId.ToString());
                trackingDataExists = coordinatorTrackingData != null;
            }

            if (trackingDataExists) return;
            using (var session = RavenDocStore.GetStore().BulkInsert(Database))
            {
                var coordinatorTrackingData = new CoordinatorTrackingData
                {
                    CoordinatorId = message.CoordinatorId,
                    CreationDateUtc = message.CreationDateUtc,
                    MetaData = message.MetaData,
                    ConfirmationEmailAddress = String.Join(", ", message.ConfirmationEmailAddresses),
                    UserOlsenTimeZone = message.UserOlsenTimeZone,
                    CurrentStatus = CoordinatorStatusTracking.Started,
                    MessageBody = message.SmsMessage,
                    MessageCount = message.SmsCount,
                    Username = message.UserName
                };
                session.Store(coordinatorTrackingData, message.CoordinatorId.ToString());
                //session.SaveChanges();
            }
        }

        public void MarkCoordinatorAsComplete(Guid coordinatorId, DateTime utcCompleteDate)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(Database))
            {
                var coordinatorTrackingData = session.Load<CoordinatorTrackingData>(coordinatorId.ToString());
                coordinatorTrackingData.CompletionDateUtc = utcCompleteDate;
                coordinatorTrackingData.CurrentStatus = CoordinatorStatusTracking.Completed;
                session.SaveChanges();
            }
        }

        public List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult> GetScheduleSummary(Guid coordinatorId)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(Database))
            {
                var coordinatorSummary = session.Query<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult, ScheduledMessagesStatusCountInCoordinatorIndex>()
                        .Customize(x => x.WaitForNonStaleResults())
                        .Where(s => s.CoordinatorId == coordinatorId.ToString())
                        .ToList();

                return coordinatorSummary;
            }
        }

        public CustomerContactList GetSmsAndEmailCoordinatorData(string smsAndEmailDataId)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Client;
using SmsMessages.CommonData;

namespace SmsTrackingModels
{
    public class CoordinatorTrackingData
    {
        public CoordinatorTrackingData()
        {
            MetaData = new SmsMetaData();
        }

        public CoordinatorTrackingData(List<MessageSendingStatus> listOfSendingStatusForTesting)
        {
            MetaData = new SmsMetaData();
            testMessageSendingStatus = listOfSendingStatusForTesting;
        }

        public Guid CoordinatorId { get; set; }

        public int MessageCount { get; set; }

        public CoordinatorStatusTracking CurrentStatus { get; set; }

        private List<MessageSendingStatus> testMessageSendingStatus { get; set; }

        public DateTime? CompletionDateUtc { get; set; }

        public DateTime CreationDateUtc { get; set; }

        public SmsMetaData MetaData { get; set; }

        public string ConfirmationEmailAddress { get; set; }

        public string UserOlsenTimeZone { get; set; }

        public string MessageBody { get; set; }

        public List<MessageSendingStatus> GetListOfCoordinatedSchedules(IDocumentStore documentStore)
        {
            if (testMessageSendingStatus != null)
                return testMessageSendingStatus;
            var trackingData = new List<MessageSendingStatus>();
            int page = 0;
            int pageSize = 100;
            RavenQueryStatistics ravenStats;

            using (var session = documentStore.OpenSession("SmsTracking"))
            {
                session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                    .Statistics(out ravenStats)
                    .FirstOrDefault(s => s.CoordinatorId == CoordinatorId);
            }

            while (ravenStats.TotalResults > (page) * pageSize)
            {
                using (var session = documentStore.OpenSession("SmsTracking"))
                {
                    var tracking = session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex")
                        .Where(s => s.CoordinatorId == CoordinatorId)
                        .Skip(pageSize * page)
                        .Take(pageSize).ToList()
                        .Select(t => new MessageSendingStatus
                        {
                            ActualSentTimeUtc = t.ConfirmationData == null ? (DateTime?)null : t.ConfirmationData.SentAtUtc,
                            Cost = t.ConfirmationData == null ? (decimal?)null : t.ConfirmationData.Price,
                            FailureData = t.SmsFailureData == null ? null : new FailureData { Message = t.SmsFailureData.Message, MoreInfo = t.SmsFailureData.MoreInfo },
                            Number = t.SmsData.Mobile,
                            ScheduleMessageId = t.ScheduleId,
                            ScheduledSendingTimeUtc = t.ScheduleTimeUtc,
                            Status = ParseMessageStatus(t.MessageStatus)
                        });
                    trackingData.AddRange(tracking);
                }
                page++;
            }
            return trackingData;
        }

        public int GetCountOfSchedules(IDocumentStore documentStore)
        {
            using (var session = documentStore.OpenSession("SmsTracking"))
            {
                return session.Query<ScheduleTrackingData>("ScheduleMessagesInCoordinatorIndex").Count(s => s.CoordinatorId == CoordinatorId);
            }
        }

        private MessageStatusTracking ParseMessageStatus(MessageStatus messageStatus)
        {
            if (messageStatus == MessageStatus.Sent)
                return MessageStatusTracking.CompletedSuccess;
            if (messageStatus == MessageStatus.Failed)
                return MessageStatusTracking.CompletedFailure;
            if (messageStatus == MessageStatus.Paused)
                return MessageStatusTracking.Paused;
            if (messageStatus == MessageStatus.WaitingForScheduling)
                return MessageStatusTracking.WaitingForScheduling;
            if (messageStatus == MessageStatus.Scheduled)
                return MessageStatusTracking.Scheduled;
            throw new NotImplementedException();
        }
    }

    public class MessageSendingStatus
    {
        public Guid ScheduleMessageId { get; set; }

        public string Number { get; set; }

        public DateTime ScheduledSendingTimeUtc { get; set; }

        public MessageStatusTracking Status { get; set; }

        public Decimal? Cost { get; set; }

        public DateTime? ActualSentTimeUtc { get; set; }

        public FailureData FailureData { get; set; }
    }

    public enum MessageStatusTracking
    {
        WaitingForScheduling,
        Scheduled,
        Paused,
        CompletedSuccess,
        CompletedFailure
    }

    public enum CoordinatorStatusTracking
    {
        Started,
        Paused,
        Completed
    }

    public class FailureData
    {
        public string Message { get; set; }

        public string MoreInfo { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using SmsMessages.CommonData;
using SmsTrackingModels;
using SmsTrackingModels.RavenIndexs;

namespace SmsWeb.Models
{
    public class CoordinatorOverview
    {
        public CoordinatorOverview() { }

        public CoordinatorOverview(CoordinatorTrackingData coordinatorTrackingData, List<ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult> coordinatorSummary)
        {
            var sentSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Sent.ToString());
            var failedSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Failed.ToString());
            var scheduledSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Scheduled.ToString());
            var cancelledSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Cancelled.ToString());
            var waitingForSchedulingSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.WaitingForScheduling.ToString());
            var pausedSummary = coordinatorSummary.FirstOrDefault(s => s.Status == MessageStatus.Paused.ToString());

            CoordinatorId = coordinatorTrackingData.CoordinatorId;
            CreationDateUtc = coordinatorTrackingData.CreationDateUtc;
            CompletionDateUtc = coordinatorTrackingData.CompletionDateUtc;
            CurrentStatus = coordinatorTrackingData.CurrentStatus;
            Topic = coordinatorTrackingData.MetaData.Topic;
            Tags = coordinatorTrackingData.MetaData.Tags;
            MessageCount = coordinatorTrackingData.MessageCount;
            MessageStatusCounter = new MessageStatusCounters
            {
                SentCount = sentSummary == null ? 0 : sentSummary.Count,
                ScheduledCount = scheduledSummary == null ? 0 : scheduledSummary.Count,
                FailedCount = failedSummary == null ? 0 : failedSummary.Count,
                CancelledCount = cancelledSummary == null ? 0 : cancelledSummary.Count,
                WaitingForSchedulingCount = waitingForSchedulingSummary == null ? 0 : waitingForSchedulingSummary.Count,
                PausedCount = pausedSummary == null ? 0 : pausedSummary.Count,
            };
            MessageBody = coordinatorTrackingData.MessageBody;
        }

        public Guid CoordinatorId { get; set; }

        public CoordinatorStatusTracking CurrentStatus { get; set; }

        public int MessageCount { get; set; }

        public DateTime CreationDateUtc { get; set; }

        public DateTime? CompletionDateUtc { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }

        public string MessageBody { get; set; }

        public MessageStatusCounters MessageStatusCounter { get; set; }

        public DateTime? NextScheduledMessageDate { get; set; }

        public DateTime? FinalScheduledMessageDate { get; set; }
    }

    public class MessageStatusCounters
    {
        public int SentCount { get; set; }

        public int ScheduledCount { get; set; }

        public int FailedCount { get; set; }

        public int CancelledCount { get; set; }

        public int WaitingForSchedulingCount { get; set; }

        public int PausedCount { get; set; }
    }

    public class CoordinatorPagedResults
    {
        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public int Page { get; set; }

        public int ResultsPerPage { get; set; }

        public List<CoordinatorOverview> CoordinatorOverviews { get; set; }
    }
}
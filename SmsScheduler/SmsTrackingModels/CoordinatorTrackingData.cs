using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsTrackingModels
{
    public class CoordinatorTrackingData
    {
        public Guid CoordinatorId { get; set; }

        public CoordinatorStatusTracking CurrentStatus { get; set; }

        public List<MessageSendingStatus> MessageStatuses { get; set; }

        public DateTime? CompletionDateUtc { get; set; }

        public DateTime CreationDateUtc { get; set; }

        public SmsMetaData MetaData { get; set; }

        public string ConfirmationEmailAddress { get; set; }
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

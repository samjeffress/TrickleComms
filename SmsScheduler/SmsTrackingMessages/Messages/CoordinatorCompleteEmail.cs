using System;
using System.Collections.Generic;

namespace SmsTrackingMessages.Messages
{
    public class CoordinatorCompleteEmail
    {
        public CoordinatorCompleteEmail()
        {
            EmailAddresses = new List<string>();
        }
        public Guid CoordinatorId { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime FinishTimeUtc { get; set; }
        public SendingData SendingData { get; set; }
        public string UserOlsenTimeZone { get; set; }
        public string Topic { get; set; }
        public List<string> EmailAddresses { get; set; }
    }

    public class SendingData
    {
        public SendingData()
        {
            SuccessfulMessages = new List<SuccessfulMessage>();
            UnsuccessfulMessageses = new List<UnsuccessfulMessage>();
        }

        public List<SuccessfulMessage> SuccessfulMessages { get; set; }

        public List<UnsuccessfulMessage> UnsuccessfulMessageses { get; set; }
    }

    public class UnsuccessfulMessage
    {
        public Guid ScheduleId { get; set; }
        public FailureReason FailureReason { get; set; }
        public DateTime ScheduleSendingTimeUtc { get; set; }
    }

    public class FailureReason
    {
        public string Message { get; set; }
        public string MoreInfo { get; set; }
    }

    public class SuccessfulMessage
    {
        public Guid ScheduleId { get; set; }
        public decimal Cost { get; set; }
        public DateTime TimeSentUtc { get; set; }
    }

}
using System;
using SmsMessages.CommonData;

namespace SmsTrackingModels
{
    public class ScheduleTrackingData
    {
        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid ScheduleId { get; set; }

        //public Guid CallerId { get; set; }

        public DateTime ScheduleTimeUtc { get; set; }

        public MessageStatus MessageStatus { get; set; }

        public SmsConfirmationData ConfirmationData { get; set; }

        public SmsFailed SmsFailureData { get; set; }

        public Guid CoordinatorId { get; set; }

        public string Username { get; set; }

        public EmailData EmailData { get; set; }

        public ScheduleType ScheduleType { get; set; }
    }

    public enum ScheduleType
    {
        Email,
        Sms
    }
}

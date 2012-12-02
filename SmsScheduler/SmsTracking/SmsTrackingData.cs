using System;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using SmsMessages.MessageSending.Events;

namespace SmsTracking
{
    public class SmsTrackingData
    {
        public SmsTrackingData() {}

        public SmsTrackingData(MessageSent message)
        {
            Status = MessageTrackedStatus.Sent;
            ConfirmationData = message.ConfirmationData;
            CorrelationId = message.CorrelationId;
            SmsData = message.SmsData;
            SmsMetaData = message.SmsMetaData;
            ConfirmationEmailAddress = message.ConfirmationEmailAddress;
        }

        public SmsTrackingData(MessageFailedSending message)
        {
            Status = MessageTrackedStatus.Failed;
            SmsFailureData = message.SmsFailed;
            CorrelationId = message.CorrelationId;
            SmsData = message.SmsData;
            SmsMetaData = message.SmsMetaData;
            ConfirmationEmailAddress = message.ConfirmationEmailAddress;
        }

        public MessageTrackedStatus Status { get; set; }

        public SmsConfirmationData ConfirmationData { get; set; }
        
        public SmsFailed SmsFailureData { get; set; }

        public Guid CorrelationId { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public string ConfirmationEmailAddress { get; set; }
    }
}
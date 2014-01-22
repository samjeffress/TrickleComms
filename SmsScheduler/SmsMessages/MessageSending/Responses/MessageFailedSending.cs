using System;
using SmsMessages.CommonData;

namespace SmsMessages.MessageSending.Responses
{
    public class MessageFailedSending
    {
        public SmsFailed SmsFailed { get; set; }

        public Guid CorrelationId { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public string ConfirmationEmailAddress { get; set; }
    }
}
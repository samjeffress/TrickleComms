using System;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.MessageSending
{
    public class MessageSent : IMessage
    {
        public SmsConfirmationData ConfirmationData { get; set; }

        public Guid CorrelationId { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public string ConfirmationEmailAddress { get; set; }
    }

    public class MessageFailedSending : IMessage
    {
        public SmsFailed SmsFailed { get; set; }

        public Guid CorrelationId { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public string ConfirmationEmailAddress { get; set; }
    }
}
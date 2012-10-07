using System;
using NServiceBus;

namespace SmsMessages
{
    public class SendOneMessageNow : ICommand
    {
        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid CorrelationId { get; set; }
    }
}

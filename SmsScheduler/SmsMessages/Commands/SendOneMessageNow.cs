using System;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.Commands
{
    public class SendOneMessageNow : ICommand
    {
        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid CorrelationId { get; set; }
    }
}

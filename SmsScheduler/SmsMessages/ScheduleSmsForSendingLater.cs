using System;
using NServiceBus;

namespace SmsMessages
{
    public class ScheduleSmsForSendingLater : ICommand
    {
        public DateTime SendMessageAt { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }
    }
}
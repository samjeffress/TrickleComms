using System;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.Commands
{
    public class ScheduleSmsForSendingLater : ICommand
    {
        public DateTime SendMessageAt { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }
    }
}
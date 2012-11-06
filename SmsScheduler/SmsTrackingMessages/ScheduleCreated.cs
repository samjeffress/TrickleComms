using System;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsTrackingMessages
{
    public class ScheduleCreated : IMessage
    {
        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid ScheduleId { get; set; }

        public Guid CallerId { get; set; }
    }
}
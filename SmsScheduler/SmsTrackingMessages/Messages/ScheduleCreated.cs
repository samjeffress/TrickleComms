using System;
using SmsMessages.CommonData;

namespace SmsTrackingMessages.Messages
{
    public class ScheduleCreated
    {
        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid ScheduleId { get; set; }

        public Guid CallerId { get; set; }
    }
}
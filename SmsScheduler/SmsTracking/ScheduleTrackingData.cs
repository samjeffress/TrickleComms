using System;
using SmsMessages.CommonData;

namespace SmsTracking
{
    public class ScheduleTrackingData
    {
        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid ScheduleId { get; set; }

        public Guid CallerId { get; set; }

        public MessageStatus MessageStatus { get; set; }
    }
}

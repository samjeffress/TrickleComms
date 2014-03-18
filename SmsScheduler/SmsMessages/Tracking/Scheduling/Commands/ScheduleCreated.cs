using System;
using SmsMessages.CommonData;

namespace SmsMessages.Tracking.Scheduling.Commands
{
    public class ScheduleCreated
    {
        public Guid ScheduleId { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public DateTime ScheduleTimeUtc { get; set; }
    }
}
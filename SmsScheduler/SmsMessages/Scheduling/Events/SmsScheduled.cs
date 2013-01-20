using System;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling.Events
{
    public class SmsScheduled
    {
        public Guid ScheduleMessageId { get; set; }

        public Guid CoordinatorId { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public DateTime ScheduleSendingTimeUtc { get; set; }
    }
}
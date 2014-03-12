using System;
using SmsMessages.CommonData;

namespace SmsMessages.Tracking.Scheduling.Commands
{
    public class ScheduleStatusChanged
    {
        public Guid ScheduleId { get; set; }

        public DateTime RequestTimeUtc { get; set; }

        public MessageStatus Status { get; set; }

        public DateTime? ScheduleTimeUtc { get; set; }
    }
}
using System;

namespace SmsMessages.Tracking.Scheduling.Commands
{
    public class ScheduleFailed
    {
        public Guid ScheduleId { get; set; }

        public string Message { get; set; }

        public string MoreInfo { get; set; }

        public string Code { get; set; }

        public string Status { get; set; }
    }
}
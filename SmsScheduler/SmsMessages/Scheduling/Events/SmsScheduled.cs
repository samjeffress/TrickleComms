using System;

namespace SmsMessages.Scheduling.Events
{
    public class SmsScheduled
    {
        public Guid ScheduleMessageId { get; set; }

        public Guid CoordinatorId { get; set; }
    }
}
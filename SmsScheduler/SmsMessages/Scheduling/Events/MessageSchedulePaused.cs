using System;

namespace SmsMessages.Scheduling.Events
{
    public class MessageSchedulePaused
    {
        public string CoordinatorId { get; set; }

        public Guid ScheduleId { get; set; }
    }
}
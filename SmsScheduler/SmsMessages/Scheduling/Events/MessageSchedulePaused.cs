using System;

namespace SmsMessages.Scheduling.Events
{
    public class MessageSchedulePaused
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduleId { get; set; }

        public string Number { get; set; }
    }
}
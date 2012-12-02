using System;

namespace SmsMessages.Scheduling.Events
{
    public class MessageRescheduled
    {
        public string CoordinatorId { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public DateTime RescheduledTimeUtc { get; set; }
    }
}
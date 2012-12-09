using System;

namespace SmsMessages.Scheduling.Events
{
    public class MessageRescheduled
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public DateTime RescheduledTimeUtc { get; set; }
    }
}
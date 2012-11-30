using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class MessageRescheduled : IMessage
    {
        public string CoordinatorId { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public DateTime RescheduledTimeUtc { get; set; }
    }
}
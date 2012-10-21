using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class MessageRescheduled : IMessage
    {
        public string CorrelationId { get; set; }

        public Guid ScheduleId { get; set; }

        public DateTime RescheduledTime { get; set; }
    }
}
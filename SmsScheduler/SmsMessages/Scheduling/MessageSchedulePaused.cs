using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class MessageSchedulePaused : IMessage
    {
        public string CorrelationId { get; set; }

        public Guid ScheduleId { get; set; }
    }
}
using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class SmsScheduled : IMessage
    {
        public Guid ScheduleMessageId { get; set; }

        public Guid CoordinatorId { get; set; }
    }
}
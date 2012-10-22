using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class MessageSchedulePaused : IMessage
    {
        public string CoordinatorId { get; set; }

        public Guid ScheduleId { get; set; }
    }
}
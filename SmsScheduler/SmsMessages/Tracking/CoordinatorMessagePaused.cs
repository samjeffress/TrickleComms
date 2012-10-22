using System;
using NServiceBus;

namespace SmsMessages.Tracking
{
    public class CoordinatorMessagePaused : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public string Number { get; set; }

        public Guid ScheduleMessageId { get; set; }
    }
}
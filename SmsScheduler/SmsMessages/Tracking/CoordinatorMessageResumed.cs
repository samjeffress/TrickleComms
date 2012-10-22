using System;
using NServiceBus;

namespace SmsMessages.Tracking
{
    public class CoordinatorMessageResumed : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public string Number { get; set; }

        public DateTime RescheduledTime { get; set; }
    }
}
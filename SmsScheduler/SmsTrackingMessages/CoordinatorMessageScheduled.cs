using System;
using NServiceBus;

namespace SmsTrackingMessages
{
    public class CoordinatorMessageScheduled : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public string Number { get; set; }

        public Guid ScheduleMessageId { get; set; }
    }
}
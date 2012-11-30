using System;
using NServiceBus;

namespace SmsTrackingMessages
{
    public class CoordinatorMessageResumed : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public string Number { get; set; }

        public DateTime RescheduledTimeUtc { get; set; }
    }
}
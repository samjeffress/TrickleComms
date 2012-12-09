using System;

namespace SmsTrackingMessages.Messages
{
    public class CoordinatorMessageSent
    {
        public Guid CoordinatorId { get; set; }

        public string Number { get; set; }

        public DateTime TimeSentUtc { get; set; }

        public decimal Cost { get; set; }

        public Guid ScheduleMessageId { get; set; }
    }
}
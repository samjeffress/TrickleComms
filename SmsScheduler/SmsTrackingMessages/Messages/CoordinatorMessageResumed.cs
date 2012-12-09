using System;

namespace SmsTrackingMessages.Messages
{
    public class CoordinatorMessageResumed
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public string Number { get; set; }

        public DateTime RescheduledTimeUtc { get; set; }
    }
}
using System;

namespace SmsTrackingMessages.Messages
{
    public class CoordinatorMessageScheduled
    {
        public Guid CoordinatorId { get; set; }

        public string Number { get; set; }

        public Guid ScheduleMessageId { get; set; }
    }
}
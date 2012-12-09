using System;

namespace SmsTrackingMessages.Messages
{
    public class ScheduleResumed
    {
        public Guid ScheduleId { get; set; }

        public DateTime RescheduledTime { get; set; }
    }
}
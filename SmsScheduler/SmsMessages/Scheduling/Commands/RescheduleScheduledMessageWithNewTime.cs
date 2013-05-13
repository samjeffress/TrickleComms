using System;

namespace SmsMessages.Scheduling.Commands
{
    public class RescheduleScheduledMessageWithNewTime
    {
        public RescheduleScheduledMessageWithNewTime(Guid scheduleMessageId, DateTime newScheduleTimeUtc)
        {
            ScheduleMessageId = scheduleMessageId;
            NewScheduleTimeUtc = newScheduleTimeUtc;
            MessageRequestTimeUtc = DateTime.Now.ToUniversalTime();
        }

        public Guid ScheduleMessageId { get; set; }
        public DateTime NewScheduleTimeUtc { get; set; }
        public DateTime MessageRequestTimeUtc { get; set; }
    }
}
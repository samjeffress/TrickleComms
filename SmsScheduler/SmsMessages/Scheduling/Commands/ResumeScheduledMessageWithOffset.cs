using System;

namespace SmsMessages.Scheduling.Commands
{
    public class ResumeScheduledMessageWithOffset
    {
        public ResumeScheduledMessageWithOffset(Guid scheduleMessageId, TimeSpan offset)
        {
            ScheduleMessageId = scheduleMessageId;
            Offset = offset;
            MessageRequestTimeUtc = DateTime.Now.ToUniversalTime();
        }

        public Guid ScheduleMessageId { get; set; }
        public TimeSpan Offset { get; set; }
        public DateTime MessageRequestTimeUtc { get; set; }
    }
}
using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class ResumeScheduledMessageWithOffset : IMessage
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
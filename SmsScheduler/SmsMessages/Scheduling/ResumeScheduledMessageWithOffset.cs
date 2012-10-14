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
        }

        public Guid ScheduleMessageId { get; set; }
        public TimeSpan Offset { get; set; }
    }
}
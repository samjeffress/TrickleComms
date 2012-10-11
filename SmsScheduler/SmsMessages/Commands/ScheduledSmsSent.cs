using System;
using NServiceBus;

namespace SmsMessages.Commands
{
    public class ScheduledSmsSent : IMessage
    {
        public Guid CoordinatorId { get; set; }
    }

    public class PauseTrickledMessagesIndefinitely : IMessage
    {
        public Guid CoordinatorId { get; set; }
    }

    public class PauseScheduledMessageIndefinitely : IMessage
    {
        public PauseScheduledMessageIndefinitely(Guid scheduleMessageId)
        {
            ScheduleMessageId = scheduleMessageId;
        }

        public Guid ScheduleMessageId { get; set; }
    }

    public class ResumeTrickledMessagesNow : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public DateTime ResumeTime { get; set; }
    }

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
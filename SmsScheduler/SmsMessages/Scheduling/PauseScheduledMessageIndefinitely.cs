using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class PauseScheduledMessageIndefinitely : IMessage
    {
        public PauseScheduledMessageIndefinitely(Guid scheduleMessageId)
        {
            ScheduleMessageId = scheduleMessageId;
        }

        public Guid ScheduleMessageId { get; set; }
    }
}
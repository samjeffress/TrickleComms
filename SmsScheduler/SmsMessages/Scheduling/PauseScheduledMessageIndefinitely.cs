using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class PauseScheduledMessageIndefinitely : IMessage
    {
        public PauseScheduledMessageIndefinitely(Guid scheduleMessageId)
        {
            ScheduleMessageId = scheduleMessageId;
            MessageRequestTimeUtc = DateTime.Now.ToUniversalTime();
        }

        public Guid ScheduleMessageId { get; set; }

        public DateTime MessageRequestTimeUtc { get; set; }
    }
}
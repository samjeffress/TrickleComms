using System;

namespace SmsMessages.Scheduling.Commands
{
    public class PauseScheduledMessageIndefinitely
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
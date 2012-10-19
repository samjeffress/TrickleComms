using System;
using System.Collections.Generic;
using NServiceBus;

namespace SmsMessages.Tracking
{
    public class CoordinatorCreated : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public List<MessageSchedule> ScheduledMessages { get; set; }
    }

    public class MessageSchedule
    {
        public string Number { get; set; }

        public DateTime ScheduledTime { get; set; }

        public Guid ScheduleMessageId { get; set; }
    }
}
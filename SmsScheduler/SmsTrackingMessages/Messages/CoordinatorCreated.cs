using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsTrackingMessages.Messages
{
    public class CoordinatorCreated
    {
        public Guid CoordinatorId { get; set; }

        public List<MessageSchedule> ScheduledMessages { get; set; }

        public DateTime CreationDate { get; set; }

        public SmsMetaData MetaData { get; set; }
    }

    public class MessageSchedule
    {
        public string Number { get; set; }

        public DateTime ScheduledTime { get; set; }

        public Guid ScheduleMessageId { get; set; }
    }
}
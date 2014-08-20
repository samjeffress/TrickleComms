using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling.Events
{
    public class EmailScheduled
    {
        public Guid ScheduleMessageId { get; set; }

        public Guid CoordinatorId { get; set; }

        public EmailData EmailData { get; set; }

        public string  Topic { get; set; }

        public List<string> Tags { get; set; }

        public DateTime ScheduleSendingTimeUtc { get; set; }

        public string Username { get; set; }
    }
}
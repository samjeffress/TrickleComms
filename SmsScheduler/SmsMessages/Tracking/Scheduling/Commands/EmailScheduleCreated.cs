using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsMessages.Tracking.Scheduling.Commands
{
    public class EmailScheduleCreated
    {
        public EmailScheduleCreated()
        {
            Tags = new List<string>();
            EmailData = new EmailData();
        }

        public Guid ScheduleId { get; set; }

        public EmailData EmailData { get; set; }

        public string Topic { get; set; }

        public List<string> Tags { get; set; }

        public DateTime ScheduleTimeUtc { get; set; }

        public Guid CorrelationId { get; set; }
    }
}
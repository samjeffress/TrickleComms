using System;

namespace SmsMessages.Scheduling.Events
{
    public class ScheduledEmailSent
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduledSmsId { get; set; }

        public EmailStatus  EmailStatus { get; set; }

        public string ToAddress { get; set; }

        public string Username { get; set; }
    }
}
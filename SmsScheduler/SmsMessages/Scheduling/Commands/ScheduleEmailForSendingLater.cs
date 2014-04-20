using System;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling.Commands
{
    public class ScheduleEmailForSendingLater
    {
        public ScheduleEmailForSendingLater()
        {}

        public ScheduleEmailForSendingLater(DateTime sendMessageAtUtc, EmailData emailData, Guid coorelationId, string username)
        {
            ScheduleMessageId = Guid.NewGuid();
            EmailData = emailData;
            SendMessageAtUtc = sendMessageAtUtc;
            CorrelationId = coorelationId;
            Username = username;
        }

        public virtual DateTime SendMessageAtUtc { get; set; }

        public virtual EmailData EmailData { get; set; }

        public virtual Guid ScheduleMessageId { get; set; }

        public virtual Guid CorrelationId { get; set; }

        public virtual string ConfirmationEmail { get; set; }

        public virtual string Username { get; set; }
    }
}
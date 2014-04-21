using System;
using System.Collections.Generic;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling.Commands
{
    public class ScheduleEmailForSendingLater
    {
        public ScheduleEmailForSendingLater()
        {
            Tags = new List<string>();
        }

        public ScheduleEmailForSendingLater(DateTime sendMessageAtUtc, EmailData emailData, SmsMetaData metaData, Guid coorelationId, string username)
        {
            ScheduleMessageId = Guid.NewGuid();
            EmailData = emailData;
            SendMessageAtUtc = sendMessageAtUtc;
            CorrelationId = coorelationId;
            Username = username;
            Tags = metaData.Tags;
            Topic = metaData.Topic;
        }

        public virtual DateTime SendMessageAtUtc { get; set; }

        public virtual EmailData EmailData { get; set; }

        public virtual Guid ScheduleMessageId { get; set; }

        public virtual Guid CorrelationId { get; set; }

        public virtual string ConfirmationEmail { get; set; }

        public virtual string Username { get; set; }

        public virtual List<string> Tags { get; set; }

        public virtual string Topic { get; set; }
    }
}
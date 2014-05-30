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

        public DateTime SendMessageAtUtc { get; set; }

        public EmailData EmailData { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public Guid CorrelationId { get; set; }

        public string ConfirmationEmail { get; set; }

        public string Username { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }
    }
}
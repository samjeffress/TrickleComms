using System;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling.Commands
{
    public class ScheduleSmsForSendingLater
    {
        public ScheduleSmsForSendingLater()
        {
            SmsData = new SmsData(string.Empty, string.Empty);
            SmsMetaData = new SmsMetaData();
        }

        public ScheduleSmsForSendingLater(DateTime sendMessageAtUtc, SmsData smsData, SmsMetaData smsMetaData, Guid coorelationId, string username)
        {
            ScheduleMessageId = Guid.NewGuid();
            SendMessageAtUtc = sendMessageAtUtc;
            SmsData = smsData;
            SmsMetaData = smsMetaData;
            CorrelationId = coorelationId;
            Username = username;
        }

        public DateTime SendMessageAtUtc { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public Guid CorrelationId { get; set; }

        public string ConfirmationEmail { get; set; }

        public string Username { get; set; }
    }
}
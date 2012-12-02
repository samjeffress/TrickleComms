using System;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling.Commands
{
    public class ScheduleSmsForSendingLater
    {
        public ScheduleSmsForSendingLater()
        {}

        public ScheduleSmsForSendingLater(DateTime sendMessageAt, SmsData smsData, SmsMetaData smsMetaData)
        {
            ScheduleMessageId = Guid.NewGuid();
            SendMessageAtUtc = sendMessageAt.ToUniversalTime();
            SmsData = smsData;
            SmsMetaData = smsMetaData;
        }

        public DateTime SendMessageAtUtc { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public Guid CorrelationId { get; set; }
    }
}
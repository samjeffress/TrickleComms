using System;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.Commands
{
    public class ScheduleSmsForSendingLater : ICommand
    {
        public ScheduleSmsForSendingLater()
        {}

        public ScheduleSmsForSendingLater(DateTime sendMessageAt, SmsData smsData, SmsMetaData smsMetaData)
        {
            ScheduleMessageId = Guid.NewGuid();
            SendMessageAt = sendMessageAt;
            SmsData = smsData;
            SmsMetaData = smsMetaData;
        }

        public DateTime SendMessageAt { get; set; }

        public SmsData SmsData { get; set; }

        public SmsMetaData SmsMetaData { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public Guid CorrelationId { get; set; }
    }
}
using System;
using SmsMessages.CommonData;

namespace SmsTrackingMessages.Messages
{
    public class CoordinatorMessageFailed
    {
        public Guid CoordinatorId { get; set; }

        public string Number { get; set; }

        public Guid ScheduleMessageId { get; set; }

        public SmsFailed SmsFailureData { get; set; }
    }
}
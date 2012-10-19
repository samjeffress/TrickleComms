using System;
using NServiceBus;
using SmsMessages.CommonData;

namespace SmsMessages.Scheduling
{
    public class ScheduledSmsSent : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduledSmsId { get; set; }

        public SmsConfirmationData ConfirmationData { get; set; }

        public string Number { get; set; }
    }
}
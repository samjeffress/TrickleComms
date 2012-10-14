using System;
using NServiceBus;

namespace SmsMessages.Scheduling
{
    public class ScheduledSmsSent : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public Guid ScheduledSmsId { get; set; }
    }
}
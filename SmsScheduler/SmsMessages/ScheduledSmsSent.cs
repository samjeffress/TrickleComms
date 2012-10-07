using System;
using NServiceBus;

namespace SmsMessages
{
    public class ScheduledSmsSent : IMessage
    {
        public Guid CoordinatorId { get; set; }
    }
}
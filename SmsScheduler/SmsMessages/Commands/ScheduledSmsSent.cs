using System;
using NServiceBus;

namespace SmsMessages.Commands
{
    public class ScheduledSmsSent : IMessage
    {
        public Guid CoordinatorId { get; set; }
    }
}
using System;
using NServiceBus;

namespace SmsMessages.Tracking
{
    public class CoordinatorCompleted : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public DateTime CompletionDate { get; set; }
    }
}
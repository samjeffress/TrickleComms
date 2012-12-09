using System;
using NServiceBus;

namespace SmsTrackingMessages.Messages
{
    public class CoordinatorCompleted : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public DateTime CompletionDate { get; set; }
    }
}
using System;
using NServiceBus;

namespace SmsMessages.Coordinator
{
    public class PauseTrickledMessagesIndefinitely : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public DateTime MessageRequestTimeUtc { get; set; }
    }
}
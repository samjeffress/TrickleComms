using System;
using NServiceBus;

namespace SmsMessages.Coordinator
{
    public class PauseTrickledMessagesIndefinitely : IMessage
    {
        public Guid CoordinatorId { get; set; }
    }
}
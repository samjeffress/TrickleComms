using System;

namespace SmsMessages.Coordinator.Commands
{
    public class PauseTrickledMessagesIndefinitely
    {
        public Guid CoordinatorId { get; set; }

        public DateTime MessageRequestTimeUtc { get; set; }
    }
}
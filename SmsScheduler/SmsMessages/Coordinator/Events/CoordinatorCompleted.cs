using System;

namespace SmsMessages.Coordinator.Events
{
    public class CoordinatorCompleted
    {
        public Guid CoordinatorId { get; set; }

        public DateTime CompletionDateUtc { get; set; }
    }
}

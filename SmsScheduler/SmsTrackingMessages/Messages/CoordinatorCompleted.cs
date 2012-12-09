using System;

namespace SmsTrackingMessages.Messages
{
    public class CoordinatorCompleted
    {
        public Guid CoordinatorId { get; set; }

        public DateTime CompletionDate { get; set; }
    }
}
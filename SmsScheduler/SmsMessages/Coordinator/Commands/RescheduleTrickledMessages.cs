using System;

namespace SmsMessages.Coordinator.Commands
{
    public class RescheduleTrickledMessages
    {
        public Guid CoordinatorId { get; set; }

        public DateTime ResumeTimeUtc { get; set; }

        public DateTime FinishTimeUtc { get; set; }

        public DateTime MessageRequestTimeUtc { get; set; }        
    }
}
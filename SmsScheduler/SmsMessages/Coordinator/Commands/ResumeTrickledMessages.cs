using System;

namespace SmsMessages.Coordinator.Commands
{
    public class ResumeTrickledMessages
    {
        public Guid CoordinatorId { get; set; }

        public DateTime ResumeTimeUtc { get; set; }

        public DateTime MessageRequestTimeUtc { get; set; }
    }
}
using System;
using NServiceBus;

namespace SmsMessages.Coordinator
{
    public class ResumeTrickledMessages : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public DateTime ResumeTimeUtc { get; set; }

        public DateTime MessageRequestTimeUtc { get; set; }
    }
}
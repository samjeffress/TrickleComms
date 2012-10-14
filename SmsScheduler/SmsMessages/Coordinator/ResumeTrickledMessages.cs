using System;
using NServiceBus;

namespace SmsMessages.Coordinator
{
    public class ResumeTrickledMessages : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public DateTime ResumeTime { get; set; }
    }
}
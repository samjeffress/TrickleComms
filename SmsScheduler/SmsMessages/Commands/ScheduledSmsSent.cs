using System;
using NServiceBus;

namespace SmsMessages.Commands
{
    public class ScheduledSmsSent : IMessage
    {
        public Guid CoordinatorId { get; set; }
    }

    public class PauseTrickledMessagesIndefinitely : IMessage
    {
        public Guid CoordinatorId { get; set; }
    }

    public class PauseScheduledMessageIndefinitely : IMessage
    {
        
    }

    public class ResumeTrickledMessagesNow : IMessage
    {
        
    }

    public class ResumeScheduledMessageWithOffset : IMessage
    {
        
    }
}
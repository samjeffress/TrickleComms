using System;
using NServiceBus;

namespace SmsTrackingMessages.Messages
{
    public class SchedulePaused : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
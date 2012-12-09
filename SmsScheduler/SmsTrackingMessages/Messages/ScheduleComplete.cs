using System;
using NServiceBus;

namespace SmsTrackingMessages.Messages
{
    public class ScheduleComplete : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
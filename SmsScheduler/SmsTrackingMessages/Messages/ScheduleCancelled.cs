using System;
using NServiceBus;

namespace SmsTrackingMessages.Messages
{
    public class ScheduleCancelled : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
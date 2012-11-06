using System;
using NServiceBus;

namespace SmsTrackingMessages
{
    public class ScheduleCancelled : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
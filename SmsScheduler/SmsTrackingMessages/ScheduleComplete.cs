using System;
using NServiceBus;

namespace SmsTrackingMessages
{
    public class ScheduleComplete : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
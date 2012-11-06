using System;
using NServiceBus;

namespace SmsTrackingMessages
{
    public class SchedulePaused : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
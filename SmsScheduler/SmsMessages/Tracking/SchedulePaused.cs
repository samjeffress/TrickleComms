using System;
using NServiceBus;

namespace SmsMessages.Tracking
{
    public class SchedulePaused : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
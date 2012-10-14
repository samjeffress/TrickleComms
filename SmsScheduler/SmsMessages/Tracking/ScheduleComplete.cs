using System;
using NServiceBus;

namespace SmsMessages.Tracking
{
    public class ScheduleComplete : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
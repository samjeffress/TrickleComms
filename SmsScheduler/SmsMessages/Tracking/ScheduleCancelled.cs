using System;
using NServiceBus;

namespace SmsMessages.Tracking
{
    public class ScheduleCancelled : IMessage
    {
        public Guid ScheduleId { get; set; }
    }
}
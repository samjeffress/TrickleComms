using System;
using NServiceBus;

namespace SmsMessages.Tracking
{
    public class CoordinatorMessageSent : IMessage
    {
        public Guid CoordinatorId { get; set; }

        public string Number { get; set; }

        public DateTime TimeSent { get; set; }

        public decimal Cost { get; set; }
    }
}
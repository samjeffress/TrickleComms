
using System;

namespace SmsMessages.MessageSending.Responses
{
    public class EmailDelivered
    {
        public string EmailId { get; set; }
        public Guid CorrelationId { get; set; }
    }

    public class EmailDeliveredAndOpened : EmailDelivered
    {
        // TODO: Add properties
    }

    public class EmailDeliveredAndClicked : EmailDelivered
    {
        // TODO: Add properties
    }

    public class EmailDeliveryFailed
    {
        public string EmailId { get; set; }

        public Guid CorrelationId { get; set; }
    }

    public class EmailUnsubscribed
    {
        public string EmailId { get; set; }

        public Guid CorrelationId { get; set; }
    }

    public class EmailComplained
    { 
        public string EmailId { get; set; }

        public Guid CorrelationId { get; set; }
    }
}
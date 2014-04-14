using System;

namespace SmsMessages.MessageSending.Commands
{
    public class SendOneEmailNow
    {
        public string ToAddress { get; set; }

        public string FromAddress { get; set; }

        public string FromDisplayName { get; set; }

        public string ReplyToAddress { get; set; }

        public string Subject { get; set; }

        public string BodyHtml { get; set; }

        public string BodyText { get; set; }

        public Guid CorrelationId { get; set; }

        public string Username { get; set; }
    }
}
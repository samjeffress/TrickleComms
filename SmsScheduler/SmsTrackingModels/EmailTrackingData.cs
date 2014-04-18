using System;

namespace SmsTrackingModels
{
    public class EmailTrackingData
    {
        // TODO: Use EmailStatus from SmsActioner
        public string EmailStatus { get; set; }

        public string EmailId { get; set; }

        public DateTime? SentTime { get; set; }

        public string ToAddress { get; set; }

        public string FromAddress { get; set; }

        public string FromDisplayName { get; set; }

        public string ReplyToAddress { get; set; }

        public string Subject { get; set; }

        public string BodyHtml { get; set; }

        public string BodyText { get; set; }

        public Guid CorrelationId { get; set; }

        public string ConfirmationEmailAddress { get; set; }

        public string Username { get; set; }
    }
}
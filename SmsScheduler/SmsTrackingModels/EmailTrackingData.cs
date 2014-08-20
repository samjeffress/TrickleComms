using System;
using SmsMessages;
using SmsMessages.MessageSending.Responses;

namespace SmsTrackingModels
{
    public class EmailTrackingData
    {
        public EmailTrackingData(EmailStatusUpdate statusUpdate)
        {
            EmailStatus = statusUpdate.Status;
            EmailId = statusUpdate.EmailId;
            //SentTime = statusUpdate - do we need the sent time??
            ToAddress = statusUpdate.ToAddress;
            FromAddress = statusUpdate.FromAddress;
            FromDisplayName = statusUpdate.FromDisplayName;
            ReplyToAddress = statusUpdate.ReplyToAddress;
            Subject = statusUpdate.Subject;
            BodyHtml = statusUpdate.BodyHtml;
            BodyText = statusUpdate.BodyText;
            CorrelationId = statusUpdate.CorrelationId;
            ConfirmationEmailAddress = statusUpdate.ConfirmationEmailAddress;
            Username = statusUpdate.Username;
        }

        public EmailTrackingData()
        { }

        public EmailStatus EmailStatus { get; set; }

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
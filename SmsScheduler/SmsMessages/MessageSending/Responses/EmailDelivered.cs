using System;
using SmsMessages.MessageSending.Commands;

namespace SmsMessages.MessageSending.Responses
{
    public class EmailStatusUpdate
    {
        [Obsolete("for testing only")]
        public EmailStatusUpdate()
        { }
        
        public EmailStatusUpdate(SendOneEmailNow originalMessage, string emailId)
        {
            EmailId = emailId;
            ToAddress = originalMessage.ToAddress;
            FromAddress = originalMessage.FromAddress;
            FromDisplayName = originalMessage.FromDisplayName;
            ReplyToAddress = originalMessage.ReplyToAddress;
            Subject = originalMessage.Subject;
            BodyHtml = originalMessage.BodyHtml;
            BodyText = originalMessage.BodyText;
            CorrelationId = originalMessage.CorrelationId;
            // TODO: Do we need email confrmation?
            //ConfirmationEmailAddress = originalMessage
            Username = originalMessage.Username;
        }

        public string EmailId { get; set; }

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

        public EmailStatus Status { get; set; }
    }

    //public class EmailDelivered : EmailStatusUpdate
    //{
    //    public EmailDelivered(SendOneEmailNow originalMessage, string emailId) : base(originalMessage, emailId)
    //    { }
    //}

    //public class EmailDeliveredAndOpened : EmailStatusUpdate
    //{
    //    public EmailDeliveredAndOpened(SendOneEmailNow originalMessage, string emailId) : base(originalMessage, emailId)
    //    { }
    //}

    //public class EmailDeliveredAndClicked : EmailStatusUpdate
    //{
    //    public EmailDeliveredAndClicked(SendOneEmailNow originalMessage, string emailId) : base(originalMessage, emailId)
    //    { }
    //}

    //public class EmailDeliveryFailed : EmailStatusUpdate
    //{
    //    public EmailDeliveryFailed(SendOneEmailNow originalMessage, string emailId) : base(originalMessage, emailId)
    //    { }
    //}

    //public class EmailUnsubscribed : EmailStatusUpdate
    //{
    //    public EmailUnsubscribed(SendOneEmailNow originalMessage, string emailId) : base(originalMessage, emailId)
    //    { }
    //}

    //public class EmailComplained : EmailStatusUpdate
    //{
    //    public EmailComplained(SendOneEmailNow originalMessage, string emailId) : base(originalMessage, emailId)
    //    { }
    //}
}
using System;

namespace SmsMessages.Email.Events
{
    public class EmailSent
    {
        public string EmailAddress { get; set; }

        public string BodyHtml { get; set; }

        public string BodyText { get; set; }

        public string Subject { get; set; }

        public Guid Id { get; set; }

        public DateTime SendTimeUtc { get; set; }
    }
}

using System;

namespace SmsActioner.InternalMessages.Responses
{
    public class EmailSent
    {
        public Guid EmailSagaId { get; set; }
        public string EmailId { get; set; }
    }
}
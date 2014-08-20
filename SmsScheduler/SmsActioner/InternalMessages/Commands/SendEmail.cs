using System;
using SmsMessages.MessageSending.Commands;

namespace SmsActioner.InternalMessages.Commands
{
    public class SendEmail
    {
        public Guid EmailSagaId { get; set; }
        public SendOneEmailNow BaseRequest { get; set; }
    }
}
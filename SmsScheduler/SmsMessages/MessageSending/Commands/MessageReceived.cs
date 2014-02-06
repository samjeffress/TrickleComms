using System;

namespace SmsMessages.MessageSending.Commands
{
    public class MessageReceived
    {
        public string Sid { get; set; }
        public DateTime DateSent { get; set; }
        public string AccountSid { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Body { get; set; }
        public decimal Price { get; set; }
    }
}
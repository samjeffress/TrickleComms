using System;

namespace SmsMessages.CommonData
{
    public abstract class SmsStatus
    {
        public string Sid { get; set; }
    }

    public class SmsFailed : SmsStatus
    {
        public SmsFailed(string sid, string code, string message)
        {
            Sid = sid;
            Code = code;
            Message = message;
        }

        public string Code { get; set; }

        public string Message { get; set; }
    }

    public class SmsSending : SmsStatus
    {
        public decimal Price { get; set; }

        public SmsSending(string sid, decimal price)
        {
            Price = price;
            Sid = sid;
        }
    }

    public class SmsQueued : SmsStatus
    {
        public SmsQueued(string sid)
        {
            Sid = sid;
        }
    }

    public class SmsSent : SmsStatus
    {
        public SmsSent(string sid, DateTime sentAtUtc)
        {
            Sid = sid;
            SentAtUtc = sentAtUtc;
        }

        public DateTime SentAtUtc { get; set; }

        //public SmsConfirmationData SmsConfirmationData { get; set; }
    }

    public class SmsConfirmationData
    {
        public SmsConfirmationData(string receipt, DateTime sentAtUtc, Decimal price)
        {
            Receipt = receipt;
            SentAtUtc = new DateTime(sentAtUtc.Ticks, DateTimeKind.Utc);
            Price = price;
        }

        public string Receipt { get; set; }

        public DateTime SentAtUtc { get; set; }

        public Decimal Price { get; set; }
    }
}
using System;

namespace SmsMessages.CommonData
{
    public abstract class SmsStatus
    {
        public string Sid { get; set; }
    }

    public class SmsConfirmationData
    {
        public SmsConfirmationData(string receipt, DateTime sentAt, Decimal price)
        {
            Receipt = receipt;
            SentAt = sentAt;
            Price = price;
        }

        public string Receipt { get; set; }

        public DateTime SentAt { get; set; }

        public Decimal Price { get; set; }
    }

    public class SmsFailed : SmsStatus
    {
        public SmsFailed(string sid, string code, string message, string moreInfo, string status)
        {
            Sid = sid;
            Code = code;
            Message = message;
            MoreInfo = moreInfo;
            Status = status;
        }

        public string Status { get; set; }

        public string MoreInfo { get; set; }

        public string Code { get; set; }

        public string Message { get; set; }
    }

    public class SmsSending : SmsStatus
    {
        public SmsSending(string sid)
        {
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
        public SmsSent(SmsConfirmationData confirmationData)
        {
            Sid = confirmationData.Receipt;
            SmsConfirmationData = confirmationData;
        }

        public SmsConfirmationData SmsConfirmationData { get; set; }
    }
}
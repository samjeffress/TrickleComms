using System;

namespace SmsMessages.CommonData
{
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
}
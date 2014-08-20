namespace SmsMessages.CommonData
{
    public class EmailData
    {
        public EmailData()
        { }

        public EmailData(EmailData baseData, string toAddress)
        {
            ToAddress = toAddress;
            FromAddress = baseData.FromAddress;
            FromDisplayName = baseData.FromDisplayName;
            ReplyToAddress = baseData.ReplyToAddress;
            Subject = baseData.Subject;
            BodyHtml = baseData.BodyHtml;
            BodyText = baseData.BodyText;
        }

        public string ToAddress { get; set; }

        public string FromAddress { get; set; }

        public string FromDisplayName { get; set; }

        public string ReplyToAddress { get; set; }

        public string Subject { get; set; }

        public string BodyHtml { get; set; }

        public string BodyText { get; set; }
    }
}
namespace SmsMessages.CommonData
{
    public class SmsData
    {
        public string Mobile { get; set; }
        public string Message { get; set; }

        public SmsMetaData MetaData { get; set; }

        public SmsData(string mobile, string message)
        {
            Mobile = mobile;
            Message = message;
        }
    }
}
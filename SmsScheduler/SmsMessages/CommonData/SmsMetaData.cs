using System.Collections.Generic;

namespace SmsMessages.CommonData
{
    public class SmsMetaData
    {
        public SmsMetaData()
        {
            Tags = new List<string>();
        }

        public string Topic { get; set; }

        public List<string> Tags { get; set; }
    }
}
using System.Collections.Generic;

namespace ConfigurationModels
{
    public class EmailDefaultNotification
    {
        public EmailDefaultNotification()
        {
            EmailAddresses = new List<string>();
        }

        public List<string> EmailAddresses { get; set; }
    }
}

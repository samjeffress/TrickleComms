using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels
{
    public class TwilioConfiguration
    {
        [Required]
        public string AuthToken { get; set; }

        [Required]
        public string AccountSid { get; set; }

        [Required]
        public string From { get; set; }
    }
}

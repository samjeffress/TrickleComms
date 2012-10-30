using System.ComponentModel.DataAnnotations;

namespace SmsCoordinator
{
    public class TwilioConfiguration
    {
        [Required]
        public string AuthToken { get; set; }

        [Required]
        public string AccountSid { get; set; }
    }
}

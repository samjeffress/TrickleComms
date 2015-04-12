using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels
{
    public class SmsProviderConfiguration
    {
        [Required]
        public SmsProvider SmsProvider { get; set; }
    }

    public enum SmsProvider
    {
        Twilio,
        Nexmo
    }
}
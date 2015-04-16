using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels.Providers
{
    public class TwilioConfiguration : ISmsProvider
    {
        [Required]
        public string AuthToken { get; set; }

        [Required]
        public string AccountSid { get; set; }

        [Required]
        public string From { get; set; }
    }

    public interface ISmsProvider
    {
        [Required]
        string From { get; set; }
    }
}

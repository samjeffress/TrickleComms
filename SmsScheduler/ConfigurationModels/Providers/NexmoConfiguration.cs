using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels.Providers
{
    public class NexmoConfiguration : ISmsProvider
    {
        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string Secret { get; set; }

        [Required]
        public string From { get; set; }
    }
}
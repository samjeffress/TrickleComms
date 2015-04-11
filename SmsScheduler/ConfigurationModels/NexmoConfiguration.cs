using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels
{
    public class NexmoConfiguration
    {
        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string Secret { get; set; }

        [Required]
        public string From { get; set; }
    }
}
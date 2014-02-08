using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels
{
    public class SmsTechConfiguration
    {
        [Required]
        public string ApiSecret { get; set; }

        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string From { get; set; }
    }
}
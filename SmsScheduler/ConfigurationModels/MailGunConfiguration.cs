using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels
{
    public class MailgunConfiguration
    {
        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string DomainName { get; set; }

        [Required]
        public string DefaultFrom { get; set; }
    }
}
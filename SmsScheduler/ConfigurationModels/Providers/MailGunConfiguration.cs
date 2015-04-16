using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels.Providers
{
    public class MailgunConfiguration : IEmailProvider
    {
        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string DomainName { get; set; }

        [Required]
        public string DefaultFrom { get; set; }
    }
}
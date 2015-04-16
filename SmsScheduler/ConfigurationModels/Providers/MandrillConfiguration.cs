using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels.Providers
{
    public class MandrillConfiguration : IEmailProvider
    {
        [Required]
        public string ApiKey { get; set; }

        [Required]
        public string DomainName { get; set; }

        [Required]
        public string DefaultFrom { get; set; }
    }

    public interface IEmailProvider
    {
        [Required]
        string ApiKey { get; set; }

        [Required]
        string DomainName { get; set; }

        [Required]
        string DefaultFrom { get; set; }
    }
}
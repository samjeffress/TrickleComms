using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels
{
    public class EmailProviderConfiguration
    {
        [Required]
        public EmailProvider EmailProvider { get; set; }
    }

    public enum EmailProvider
    {
        Mandrill,
        Mailgun
    }
}
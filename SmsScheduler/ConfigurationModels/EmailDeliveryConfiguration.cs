using System.ComponentModel.DataAnnotations;

namespace ConfigurationModels
{
    public class EmailDeliveryConfiguration
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
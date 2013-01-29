using System.ComponentModel.DataAnnotations;

namespace SmsWeb.Models
{
    public class DefaultEmailModel
    {
        [Required]
        public string DefaultEmails { get; set; }
    }
}
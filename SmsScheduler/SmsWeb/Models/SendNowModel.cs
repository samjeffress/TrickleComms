using System.ComponentModel.DataAnnotations;

namespace SmsWeb.Models
{
    public class SendNowModel
    {
        [Required]
        public string Number { get; set; }

        [Required]
        public string MessageBody { get; set; }

        public string ConfirmationEmail { get; set; }
    }
}
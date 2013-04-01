using System;
using System.ComponentModel.DataAnnotations;

namespace SmsWeb.Models
{
    public class SendNowModel
    {
        public Guid MessageId { get; set; }

        [Required]
        public string Number { get; set; }

        [Required]
        public string MessageBody { get; set; }

        public string ConfirmationEmail { get; set; }

        public string Topic { get; set; }

        public string Tags { get; set; }
    }
}
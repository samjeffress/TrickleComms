using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmsWeb.Models
{
    public class CoordinatedSharedMessageModel
    {
        [Required]
        public string Numbers { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public int? TimeSeparatorSeconds { get; set; }

        public DateTime? SendAllBy { get; set; }

        public string Tags { get; set; }

        public string Topic { get; set; }

        public string ConfirmationEmail { get; set; }
    }
}
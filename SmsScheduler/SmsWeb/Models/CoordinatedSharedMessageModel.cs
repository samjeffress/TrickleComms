using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmsWeb.Models
{
    public class CoordinatedSharedMessageModel
    {
        [Required]
        public List<string> Numbers { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public TimeSpan? TimeSeparator { get; set; }

        public DateTime? SendAllBy { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }
    }
}
using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

namespace SmsWeb.Models
{
    public class CoordinatedSharedMessageModelFromCsv
    {
        [Required]
        public string Message { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        public int? TimeSeparatorSeconds { get; set; }

        public DateTime? SendAllBy { get; set; }

        public string Tags { get; set; }

        public string Topic { get; set; }

        public string ConfirmationEmail { get; set; }

        [Required]
        [FileTypes("csv,xls,xlsx")]
        public HttpPostedFileBase UploadedFile { get; set; }
    }
}
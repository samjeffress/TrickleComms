using System;

namespace SmsWeb.Models
{
    public class ScheduleFailedModel
    {
        public Guid ScheduleId { get; set; }

        public string Number { get; set; }

        public string ErrorMessage { get; set; }

        public string ErrorCode { get; set; }
    }
}
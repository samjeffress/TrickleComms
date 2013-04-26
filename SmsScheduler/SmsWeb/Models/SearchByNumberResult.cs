using System;

namespace SmsWeb.Models
{
    public class SearchByNumberResult
    {
        public string PhoneNumber { get; set; }

        public DateTime? SendingDate { get; set; }

        public string Status { get; set; }

        public string Topic { get; set; }

        public string CoordinatorId { get; set; }
    }
}
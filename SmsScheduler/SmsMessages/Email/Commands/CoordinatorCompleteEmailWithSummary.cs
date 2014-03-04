using System;
using System.Collections.Generic;

namespace SmsMessages.Email.Commands
{
    public class CoordinatorCompleteEmailWithSummary
    {
        public CoordinatorCompleteEmailWithSummary()
        {
            EmailAddresses = new List<string>();
        }
        public Guid CoordinatorId { get; set; }
        public DateTime StartTimeUtc { get; set; }
        public DateTime FinishTimeUtc { get; set; }
        public string UserOlsenTimeZone { get; set; }
        public string Topic { get; set; }
        public List<string> EmailAddresses { get; set; }
        public int FailedCount { get; set; }
        public int SuccessCount { get; set; }
        public decimal Cost { get; set; }
        public string UserName { get; set; }
    }
}

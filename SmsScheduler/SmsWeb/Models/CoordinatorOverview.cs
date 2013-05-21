using System;
using System.Collections.Generic;
using SmsTrackingModels;

namespace SmsWeb.Models
{
    public class CoordinatorOverview
    {
        public Guid CoordinatorId { get; set; }

        public CoordinatorStatusTracking CurrentStatus { get; set; }

        public int MessageCount { get; set; }

        public DateTime CreationDateUtc { get; set; }

        public DateTime? CompletionDateUtc { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }

        public List<StatusCounter> StatusCounters { get; set; }

        public string MessageBody { get; set; }
    }

    public class StatusCounter
    {
        public string Status { get; set; }
        public int Count { get; set; }
        public decimal Cost { get; set; }
    }

    public class CoordinatorPagedResults
    {
        public int TotalResults { get; set; }

        public int TotalPages { get; set; }

        public int Page { get; set; }

        public int ResultsPerPage { get; set; }

        public List<CoordinatorOverview> CoordinatorOverviews { get; set; }
    }
}
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

        public DateTime CreationDate { get; set; }

        public DateTime? CompletionDate { get; set; }

        public List<string> Tags { get; set; }

        public string Topic { get; set; }
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
using System;
using System.Collections.Generic;

namespace SmsWeb.Models
{
    public class PagedResult<T>
    {
        public int TotalPages { get; set; }

        public int ResultsPerPage { get; set; }

        public int CurrentPage { get; set; }

        public List<T> ResultsList { get; set; }

        public Guid CoordinatorId { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ConfigurationModels;

namespace SmsWeb.Models
{
    public class CoordinatedSharedMessageModel
    {
        public CoordinatedSharedMessageModel()
        {
            CoordinatorsToExclude = new List<Guid>();
        }

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

        public List<Guid> CoordinatorsToExclude { get; set; }

        public List<string> GetTagList()
        {
            return string.IsNullOrWhiteSpace(Tags) ? null : Tags.Split(',').ToList().Select(t => t.Trim()).ToList();
        }

        public List<string> GetCleanInternationalisedNumbers(CountryCodeReplacement countryCodeReplacement)
        {
            return Numbers.Split(',').Select(number => countryCodeReplacement != null ? countryCodeReplacement.CleanAndInternationaliseNumber(number) : number.Trim()).ToList();
        }
    }
}
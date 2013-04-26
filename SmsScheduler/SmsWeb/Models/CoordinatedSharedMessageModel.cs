using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ConfigurationModels;
using SmsMessages.Coordinator.Commands;

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

        [Required]
        public string UserTimeZone { get; set; }

        public int? TimeSeparatorSeconds { get; set; }

        public DateTime? SendAllBy { get; set; }

        public bool? SendAllAtOnce { get; set; }

        public string Tags { get; set; }

        public string Topic { get; set; }

        public string ConfirmationEmail { get; set; }

        public List<Guid> CoordinatorsToExclude { get; set; }

        public List<string> GetTagList()
        {
            return string.IsNullOrWhiteSpace(Tags) ? null : Tags.Split(new[] { ',', ';', ':' }).ToList().Select(t => t.Trim()).ToList();
        }

        public List<string> GetEmailList()
        {
            return string.IsNullOrWhiteSpace(ConfirmationEmail) ? null : ConfirmationEmail.Split(new[] {',', ';', ':'}).ToList().Select(t => t.Trim()).ToList();
        }

        public List<string> GetCleanInternationalisedNumbers(CountryCodeReplacement countryCodeReplacement)
        {
            return Numbers.Split(new[] { ',', ';', ':' }).Select(number => countryCodeReplacement != null ? countryCodeReplacement.CleanAndInternationaliseNumber(number) : number.Trim()).ToList();
        }

        public bool IsMessageTypeValid()
        {
            try
            {
                GetMessageTypeFromModel();
                return true;
            }
            catch
            {
                return false;
            }
        }

        // TODO : Add tests for this method
        public Type GetMessageTypeFromModel()
        {
            Type requestType = typeof(object);
            var trueCount = 0;
            if (SendAllBy.HasValue)
            {
                requestType = typeof(TrickleSmsOverCalculatedIntervalsBetweenSetDates);
                trueCount++;
            }
            if (TimeSeparatorSeconds.HasValue)
            {
                requestType = typeof(TrickleSmsWithDefinedTimeBetweenEachMessage);
                trueCount++;
            }
            if (SendAllAtOnce.GetValueOrDefault() || Numbers.Split(',').Count() == 1)
            {
                requestType = typeof(SendAllMessagesAtOnce);
                trueCount++;
            }
            if (trueCount != 1)
                throw new ArgumentException("Cannot determine which message type to send");
            return requestType;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using ConfigurationModels;
using SmsMessages.Coordinator.Commands;

namespace SmsWeb.Models
{
    public class CoordinatedSharedMessageModel : CoordinatorTypeModel
    {
        public CoordinatedSharedMessageModel()
        {
            CoordinatorsToExclude = new List<Guid>();
        }

        [Required]
        public string Numbers { get; set; }

        [Required]
        public string Message { get; set; }

        public List<string> GetCleanInternationalisedNumbers(CountryCodeReplacement countryCodeReplacement)
        {
            return Numbers.Split(new[] { ',', ';', ':' }).Select(number => countryCodeReplacement != null ? countryCodeReplacement.CleanAndInternationaliseNumber(number) : number.Trim()).ToList();
        }

        // TODO : Add tests for this method
        public override Type GetMessageTypeFromModel()
        {
            Type requestType = typeof(object);
            var trueCount = 0;
            if (SendAllBy.HasValue)
            {
                requestType = typeof(TrickleSmsOverCalculatedIntervalsBetweenSetDates);
                trueCount++;
            }
            else if (SendAllAtOnce.GetValueOrDefault() || Numbers.Split(',').Count() == 1)
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
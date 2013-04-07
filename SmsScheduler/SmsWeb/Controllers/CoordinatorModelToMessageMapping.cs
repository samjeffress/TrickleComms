using System;
using System.Collections.Generic;
using System.Linq;
using ConfigurationModels;
using NodaTime;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public interface ICoordinatorModelToMessageMapping
    {
        TrickleSmsOverCalculatedIntervalsBetweenSetDates MapToTrickleOverPeriod(CoordinatedSharedMessageModel model, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers);

        TrickleSmsWithDefinedTimeBetweenEachMessage MapToTrickleSpacedByPeriod(CoordinatedSharedMessageModel model, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers);
        
        SendAllMessagesAtOnce MapToSendAllAtOnce(CoordinatedSharedMessageModel coordinatedSharedMessageModel, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers);
    }

    public class CoordinatorModelToMessageMapping : ICoordinatorModelToMessageMapping
    {
        public DateTime DateTimeWithOlsenZoneToUtc(DateTime dateTime, string olsenTimeZone)
        {
            var dateTimeZoneProvider = DateTimeZoneProviders.Tzdb;
            var dateTimeZone = dateTimeZoneProvider[olsenTimeZone];
            var startTime = dateTime;
            var localDateTime = new LocalDateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute);
            return dateTimeZone.AtLeniently(localDateTime).ToDateTimeUtc();
        }

        public TrickleSmsOverCalculatedIntervalsBetweenSetDates MapToTrickleOverPeriod(CoordinatedSharedMessageModel model, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers)
        {
            return new TrickleSmsOverCalculatedIntervalsBetweenSetDates
                {
                    Duration = model.SendAllBy.Value.Subtract(model.StartTime),
                    Messages = model
                                    .GetCleanInternationalisedNumbers(countryCodeReplacement)
                                    .Where(n => !excludedNumbers.Contains(n))
                                    .Select(n => new SmsData(n, model.Message))
                                    .ToList(),
                    StartTimeUtc = DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone), // startTimeUtc,// model.StartTime.ToUniversalTime(),
                    MetaData = new SmsMetaData
                        {
                            Tags = model.GetTagList(), 
                            Topic = model.Topic
                        },
                    ConfirmationEmail = model.ConfirmationEmail
                };
        }

        public TrickleSmsWithDefinedTimeBetweenEachMessage MapToTrickleSpacedByPeriod(CoordinatedSharedMessageModel model, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers)
        {
            return new TrickleSmsWithDefinedTimeBetweenEachMessage
                {
                    Messages = model
                                    .GetCleanInternationalisedNumbers(countryCodeReplacement)
                                    .Where(n => !excludedNumbers.Contains(n))
                                    .Select(n => new SmsData(n, model.Message))
                                    .ToList(),
                    StartTimeUtc = model.StartTime.ToUniversalTime(),
                    TimeSpacing = TimeSpan.FromSeconds(model.TimeSeparatorSeconds.Value),
                    MetaData = new SmsMetaData { Tags = model.GetTagList(), Topic = model.Topic },
                    ConfirmationEmail = model.ConfirmationEmail
                };
        }

        public SendAllMessagesAtOnce MapToSendAllAtOnce(CoordinatedSharedMessageModel model, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers)
        {
            return new SendAllMessagesAtOnce
            {
                Messages = model
                                .GetCleanInternationalisedNumbers(countryCodeReplacement)
                                .Where(n => !excludedNumbers.Contains(n))
                                .Select(n => new SmsData(n, model.Message))
                                .ToList(),
                SendTimeUtc = model.StartTime.ToUniversalTime(),
                MetaData = new SmsMetaData { Tags = model.GetTagList(), Topic = model.Topic },
                ConfirmationEmail = model.ConfirmationEmail
            };
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using ConfigurationModels;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;
using SmsWeb.Models;

namespace SmsWeb.Controllers
{
    public interface ICoordinatorModelToMessageMapping
    {
        TrickleSmsOverCalculatedIntervalsBetweenSetDates MapToTrickleOverPeriod(CoordinatedSharedMessageModel model, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers);

        TrickleSmsWithDefinedTimeBetweenEachMessage MapToTrickleSpacedByPeriod(CoordinatedSharedMessageModel model, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers, string username);

        SendAllMessagesAtOnce MapToSendAllAtOnce(CoordinatedSharedMessageModel coordinatedSharedMessageModel, CountryCodeReplacement countryCodeReplacement, List<string> excludedNumbers, string username);

        TrickleSmsAndEmailBetweenSetTimes MapToTrickleSmsAndEmailOverPeriod(Guid trickleId, string customerContactsId, CoordinatorSmsAndEmailModel model, string username);
    }

    public class CoordinatorModelToMessageMapping : ICoordinatorModelToMessageMapping
    {
        public CoordinatorModelToMessageMapping(IDateTimeUtcFromOlsenMapping dateTimeUtcFromOlsenMapping)
        {
            DateTimeOlsenMapping = dateTimeUtcFromOlsenMapping;
        }

        private IDateTimeUtcFromOlsenMapping DateTimeOlsenMapping { get; set; }

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
                    StartTimeUtc = DateTimeOlsenMapping.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone), // startTimeUtc,// model.StartTime.ToUniversalTime(),
                    MetaData = new SmsMetaData
                        {
                            Tags = model.GetTagList(), 
                            Topic = model.Topic
                        },
                    ConfirmationEmail = model.ConfirmationEmail,
                    ConfirmationEmails = model.GetEmailList(),
                    UserOlsenTimeZone = model.UserTimeZone
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
                    StartTimeUtc = DateTimeOlsenMapping.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone),
                    TimeSpacing = TimeSpan.FromSeconds(model.TimeSeparatorSeconds.Value),
                    MetaData = new SmsMetaData { Tags = model.GetTagList(), Topic = model.Topic },
                    ConfirmationEmail = model.ConfirmationEmail,
                    ConfirmationEmails = model.GetEmailList(),
                    UserOlsenTimeZone = model.UserTimeZone
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
                SendTimeUtc = DateTimeOlsenMapping.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone),
                MetaData = new SmsMetaData { Tags = model.GetTagList(), Topic = model.Topic },
                ConfirmationEmail = model.ConfirmationEmail,
                ConfirmationEmails = model.GetEmailList(),
                UserOlsenTimeZone = model.UserTimeZone
            };
        }

        public TrickleSmsAndEmailBetweenSetTimes MapToTrickleSmsAndEmailOverPeriod(Guid trickleId, string customerContactsId, CoordinatorSmsAndEmailModel model, string username)
        {
            var mapToTrickleSmsAndEmailOverPeriod = new TrickleSmsAndEmailBetweenSetTimes
                {
                    ConfirmationEmails = new List<string> {model.ConfirmationEmail },
                    CoordinatorId = trickleId,
                    MetaData = new SmsMetaData { Topic = model.Topic, Tags = model.GetTagList() },
                    StartTimeUtc = DateTimeOlsenMapping.DateTimeWithOlsenZoneToUtc(model.StartTime, model.UserTimeZone),
                    Duration = model.SendAllBy.Value.Subtract(model.StartTime),
                    EmailData = new EmailData
                        {
                            BodyHtml = model.EmailHtmlContent,
                            FromAddress = "samjeffress@gmail.com", // TODO: Get from details from config??
                            BodyText = string.Empty,
                            FromDisplayName = "Sam Jeffress Test",
                            ReplyToAddress = "samjeffress@gmail.com",
                            Subject = "test"
                        },
                    UserOlsenTimeZone = model.UserTimeZone,
                    Username = username,
                    SmsAndEmailDataId = customerContactsId,
                    SmsMessage = model.SmsContent
                };
            return mapToTrickleSmsAndEmailOverPeriod;
        }
    }
}

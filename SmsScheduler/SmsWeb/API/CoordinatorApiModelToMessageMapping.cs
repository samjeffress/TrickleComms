using System;
using System.Linq;
using SmsMessages.CommonData;
using SmsMessages.Coordinator.Commands;

namespace SmsWeb.API
{
    public interface ICoordinatorApiModelToMessageMapping
    {
        TrickleSmsOverCalculatedIntervalsBetweenSetDates MapToTrickleOverPeriod(Coordinator model, Guid requestId);

        SendAllMessagesAtOnce MapToSendAllAtOnce(Coordinator model, Guid requestId);
    }

    public class CoordinatorApiModelToMessageMapping : ICoordinatorApiModelToMessageMapping
    {
        public TrickleSmsOverCalculatedIntervalsBetweenSetDates MapToTrickleOverPeriod(Coordinator model, Guid requestId)
        {
            return new TrickleSmsOverCalculatedIntervalsBetweenSetDates
            {
                Duration = model.SendAllByUtc.Value.Subtract(model.StartTimeUtc),
                Messages =
                    model.Numbers.Select(n => new SmsData(n, model.Message)).
                    ToList(),
                StartTimeUtc = model.StartTimeUtc.ToUniversalTime(),
                MetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic },
                CoordinatorId = requestId,
                ConfirmationEmails = model.ConfirmationEmails,
                UserOlsenTimeZone = string.IsNullOrWhiteSpace(model.OlsenTimeZone) ? "UTC" : model.OlsenTimeZone
            };
        }

        public SendAllMessagesAtOnce MapToSendAllAtOnce(Coordinator model, Guid requestId)
        {
            return new SendAllMessagesAtOnce
            {
                Messages =
                    model.Numbers.Select(n => new SmsData(n, model.Message)).
                    ToList(),
                SendTimeUtc = model.StartTimeUtc.ToUniversalTime(),
                MetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic },
                CoordinatorId = requestId,
                ConfirmationEmails = model.ConfirmationEmails,
                UserOlsenTimeZone = string.IsNullOrWhiteSpace(model.OlsenTimeZone) ? "UTC" : model.OlsenTimeZone
            };
        }
    }
}
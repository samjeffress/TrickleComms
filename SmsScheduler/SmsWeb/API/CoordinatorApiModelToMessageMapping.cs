using System;
using System.Linq;
using SmsMessages.CommonData;
using SmsMessages.Coordinator;
using SmsMessages.Coordinator.Commands;

namespace SmsWeb.API
{
    public interface ICoordinatorApiModelToMessageMapping
    {
        TrickleSmsOverCalculatedIntervalsBetweenSetDates MapToTrickleOverPeriod(Coordinator model, Guid requestId);

        TrickleSmsWithDefinedTimeBetweenEachMessage MapToTrickleSpacedByPeriod(Coordinator model, Guid requestId);
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
                CoordinatorId = requestId
            };
        }

        public TrickleSmsWithDefinedTimeBetweenEachMessage MapToTrickleSpacedByPeriod(Coordinator model, Guid requestId)
        {
            return new TrickleSmsWithDefinedTimeBetweenEachMessage
            {
                Messages =
                    model.Numbers.Select(n => new SmsData(n, model.Message)).
                    ToList(),
                StartTimeUTC = model.StartTimeUtc.ToUniversalTime(),
                TimeSpacing = model.TimeSeparator.Value,
                MetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic },
                CoordinatorId = requestId
            };
        }
    }
}
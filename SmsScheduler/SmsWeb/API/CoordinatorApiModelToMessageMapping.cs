using System;
using System.Linq;
using SmsMessages.CommonData;
using SmsMessages.Coordinator;

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
                Duration = model.SendAllBy.Value.Subtract(model.StartTime),
                Messages =
                    model.Numbers.Select(n => new SmsData(n, model.Message)).
                    ToList(),
                StartTime = model.StartTime,
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
                StartTime = model.StartTime,
                TimeSpacing = model.TimeSeparator.Value,
                MetaData = new SmsMetaData { Tags = model.Tags, Topic = model.Topic },
                CoordinatorId = requestId
            };
        }
    }
}
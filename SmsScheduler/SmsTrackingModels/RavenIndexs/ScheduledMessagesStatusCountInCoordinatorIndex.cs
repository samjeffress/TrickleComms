using System;
using System.Linq;
using Raven.Client.Indexes;

namespace SmsTrackingModels.RavenIndexs
{
    public class ScheduledMessagesStatusCountInCoordinatorIndex : AbstractIndexCreationTask<ScheduleTrackingData, ScheduledMessagesStatusCountInCoordinatorIndex.ReduceResult>
    {
        public ScheduledMessagesStatusCountInCoordinatorIndex()
        {
            Map = schedules => from schedule in schedules
                               select
                                   new ReduceResult
                                       {
                                           CoordinatorId = schedule.CoordinatorId.ToString(),
                                           Status = schedule.MessageStatus.ToString(),
                                           Topic = schedule.SmsMetaData.Topic,
                                           //PhoneNumber = schedule.SmsData == null ? null : schedule.SmsData.Mobile,
                                           //EmailAddress = schedule.EmailData == null ? null : schedule.EmailData.ToAddress,
                                           Type = schedule.ScheduleType,
                                           Count = 1,
                                           Cost = schedule.ConfirmationData == null ? 0 : schedule.ConfirmationData.Price
                                       };

            Reduce = results => from result in results
                                group result by new
                                    {
                                        result.Topic,
                                        result.CoordinatorId,
                                        result.Status,
                                        result.Type
                                        //result.PhoneNumber,
                                        //result.EmailAddress
                                    }
                                into g
                                select new ReduceResult
                                    {
                                        Topic = g.Key.Topic,
                                        CoordinatorId = g.Key.CoordinatorId,
                                        Status = g.Key.Status,
                                        Type = g.Key.Type,
                                        //PhoneNumber = g.Key.PhoneNumber,
                                        //EmailAddress = g.Key.EmailAddress,
                                        Count = g.Sum(x => x.Count),
                                        Cost = g.Sum(x => x.Cost)
                                    };
        }

        public class ReduceResult
        {
            public string PhoneNumber { get; set; }

            public DateTime SendingDate { get; set; }

            public string Status { get; set; }

            public string Topic { get; set; }

            public string CoordinatorId { get; set; }

            public int Count { get; set; }

            public decimal Cost { get; set; }

            public string EmailAddress { get; set; }

            public ScheduleType Type { get; set; }
        }
    }
}
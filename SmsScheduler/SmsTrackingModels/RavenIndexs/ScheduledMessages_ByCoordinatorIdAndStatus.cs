using System.Linq;
using Raven.Client.Indexes;

namespace SmsTrackingModels.RavenIndexs
{
    public class ScheduledMessages_ByCoordinatorIdAndStatus : AbstractIndexCreationTask<ScheduleTrackingData, ScheduledMessages_ByCoordinatorIdAndStatus.ReduceResult>
    {
        public ScheduledMessages_ByCoordinatorIdAndStatus()
        {
            Map = schedules => from schedule in schedules
                               select
                                   new ReduceResult
                                       {
                                           CoordinatorId = schedule.CoordinatorId.ToString(),
                                           Status = schedule.MessageStatus.ToString(),
                                           Count = 1,
                                       };

            Reduce = results => from result in results
                                group result by new
                                    {
                                        result.CoordinatorId,
                                        result.Status,
                                    }
                                into g
                                select new ReduceResult
                                    {
                                        CoordinatorId = g.Key.CoordinatorId,
                                        Status = g.Key.Status,
                                        Count = g.Sum(x => x.Count)
                                    };
        }

        public class ReduceResult
        {
            public string Status { get; set; }

            public string CoordinatorId { get; set; }

            public int Count { get; set; }
        }
    }
}
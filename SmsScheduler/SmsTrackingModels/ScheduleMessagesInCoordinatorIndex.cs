using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace SmsTrackingModels
{
    public class ScheduleMessagesInCoordinatorIndex : AbstractIndexCreationTask<ScheduleTrackingData>
    {
        public ScheduleMessagesInCoordinatorIndex()
        {
            Map = schedules => from schedule in schedules
                               select new
                                   {
                                       CoordinatorId = schedule.CoordinatorId.ToString(),
                                       MessageStatus = schedule.MessageStatus.ToString(),
                                       ScheduleId = schedule.ScheduleId.ToString()
                                   };

            Indexes.Add(s => s.CoordinatorId, FieldIndexing.Analyzed);
            Indexes.Add(s => s.MessageStatus, FieldIndexing.Analyzed);
            Indexes.Add(s => s.ScheduleId, FieldIndexing.Analyzed);
        }
    }
}
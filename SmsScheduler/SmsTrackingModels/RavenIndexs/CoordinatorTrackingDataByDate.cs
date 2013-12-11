using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace SmsTrackingModels.RavenIndexs
{
    public class CoordinatorTrackingDataByDate : AbstractIndexCreationTask<CoordinatorTrackingData>
    {
        public CoordinatorTrackingDataByDate()
        {
            Map = coordinators => from coordinator in coordinators
                select new
                {
                    CreationDate = coordinator.CreationDateUtc,
                    Status = coordinator.CurrentStatus
                };
            Indexes.Add(c => c.CreationDateUtc, FieldIndexing.Default);
            Indexes.Add(c => c.CurrentStatus, FieldIndexing.Default);
        }
    }
}
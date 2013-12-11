using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace SmsTrackingModels.RavenIndexs
{
    public class CoordinatorTagList : AbstractIndexCreationTask<CoordinatorTrackingData, CoordinatorTagList.ReduceResult>
    {
        public CoordinatorTagList()
        {
            Map = coordinators => from coordinator in coordinators
                                  let tags = coordinator.MetaData.Tags
                                  where tags != null
                                  from tag in tags
                                  select new { Tag = tag.ToLower().ToString(), Count = 1 };


            Reduce = results => from result in results
                                group result by result.Tag
                                into g
                                select new
                                    {
                                        Tag = g.Key,
                                        Count = g.Sum(x => x.Count)
                                    };

            Stores.Add(x => x.Tag, FieldStorage.Yes);
            Indexes.Add(x => x.Tag, FieldIndexing.Analyzed);
        }

        public class ReduceResult
        {
            public string Tag { get; set; }

            public int Count { get; set; }
        }
    }
}
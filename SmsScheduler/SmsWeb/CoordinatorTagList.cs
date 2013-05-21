using System;
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;
using SmsTrackingModels;

namespace SmsWeb
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

    public class PhoneNumberInCoordinatedMessages : AbstractIndexCreationTask<ScheduleTrackingData, PhoneNumberInCoordinatedMessages.ReduceResult>
    {
        public PhoneNumberInCoordinatedMessages()
        {
            Map = schedules => 
                                  from schedule in schedules
                                  select
                                      new ReduceResult
                                          {
                                              PhoneNumber = schedule.SmsData.Mobile,
                                              Topic = schedule.SmsMetaData.Topic,
                                              CoordinatorId = schedule.CoordinatorId.ToString(),
                                              SendingDate = schedule.ScheduleTimeUtc,
                                              Status = schedule.MessageStatus.ToString(),
                                              Count = 1
                                          };

            Reduce = results => from result in results
                                group result by new 
                                    {
                                        result.PhoneNumber, 
                                        result.Topic, 
                                        result.CoordinatorId, 
                                        result.SendingDate, 
                                        result.Status,
                                    }
                                into g
                                select new ReduceResult
                                    {
                                        PhoneNumber = g.Key.PhoneNumber,
                                        Topic = g.Key.Topic,
                                        CoordinatorId = g.Key.CoordinatorId,
                                        SendingDate = g.Key.SendingDate,
                                        Status = g.Key.Status,
                                        Count = g.Sum(x => x.Count)
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
        }
    }
}
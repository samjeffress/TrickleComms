using System;
using System.Collections.Generic;
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;
using Raven.Client.Indexes;
using SmsTrackingModels;

namespace SmsCoordinator
{
    public interface IRavenDocStore
    {
        IDocumentStore GetStore();
    }
 
    public class RavenDocStore : IRavenDocStore
    {
        private readonly IDocumentStore _documentStore;
        public RavenDocStore()
        {
            _documentStore = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "SmsTracking", ResourceManagerId = Guid.NewGuid() };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("Configuration");
            IndexCreation.CreateIndexes(typeof(ScheduleMessagesInCoordinatorIndex).Assembly, _documentStore);
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }

    public class ScheduleMessagesInCoordinatorIndex : AbstractIndexCreationTask<ScheduleTrackingData>
    {
        public ScheduleMessagesInCoordinatorIndex()
        {
            Map = schedules => from schedule in schedules
                               select new
                                          {
                                              CoordinatorId = schedule.CoordinatorId.ToString(),
                                              MessageStatus = schedule.MessageStatus.ToString()
                                          };

            Indexes.Add(s => s.CoordinatorId, FieldIndexing.Analyzed);
            Indexes.Add(s => s.MessageStatus, FieldIndexing.Analyzed);
        }
    }


    public class ScheduledMessagesStatusCountInCoordinatorIndex : AbstractIndexCreationTask<ScheduleTrackingData, ScheduledMessagesStatusCountInCoordinatorIndex .ReduceResult>
    {
        public ScheduledMessagesStatusCountInCoordinatorIndex ()
        {
            Map = schedules => from schedule in schedules
                                  select
                                      new ReduceResult
                                      {
                                          CoordinatorId = schedule.CoordinatorId.ToString(),
                                          Status = schedule.MessageStatus.ToString(),
                                          Topic = schedule.SmsMetaData.Topic,
                                          Count = 1,
                                          Cost = schedule.ConfirmationData == null ? 0 : schedule.ConfirmationData.Price
                                      };

            Reduce = results => from result in results
                                group result by new 
                                {
                                    result.Topic,
                                    result.CoordinatorId,
                                    result.Status,
                                }
                                    into g
                                    select new ReduceResult
                                    {
                                        Topic = g.Key.Topic,
                                        CoordinatorId = g.Key.CoordinatorId,
                                        Status = g.Key.Status,
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
        }
    }

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
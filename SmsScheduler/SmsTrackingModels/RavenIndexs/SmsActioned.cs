using System;
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace SmsTrackingModels.RavenIndexs
{
    public class SmsActioned : AbstractMultiMapIndexCreationTask<SmsActioned.Result>
    {
        public class Result
        {
            public DateTime ActionTime { get; set; }

            public string Number { get; set; }

            public int Count { get; set; }
            public string Status { get; set; }
            public string Topic { get; set; }
        }

        public SmsActioned()
        {
            AddMap<SmsTrackingData>(datas =>
              from data in datas
              select
                  new Result
                  {
                      Number = data.SmsData.Mobile,
                      Topic = data.SmsMetaData.Topic,
                      ActionTime = data.ConfirmationData.SentAtUtc,
                      Status = data.Status.ToString(),
                      Count = 1
                  });

            AddMap<SmsReceivedData>(messages => from message in messages
                select new Result
                {
                    ActionTime = message.SmsConfirmationData.SentAtUtc,
                    Number = message.SmsData.Mobile,
                    Topic = string.Empty,
                    Status = "Received",
                    Count = 1
                });

            Reduce = results => from result in results
                                group result by new
                                {
                                    result.Number,
                                    result.Topic,
                                    result.ActionTime,
                                    result.Status,
                                }
                                    into g
                                    select new Result
                                    {
                                        Number = g.Key.Number,
                                        Topic = g.Key.Topic,
                                        ActionTime = g.Key.ActionTime,
                                        Status = g.Key.Status,
                                        Count = g.Sum(x => x.Count)
                                    };

            

            //AddMap<SmsTrackingData>(messages => from message in messages 
            //    select new //Result
            //    {
            //        ActionTime = message.ConfirmationData.SentAtUtc,
            //        Number = message.SmsData.Mobile,
            //    });

            //Index(a => a.ActionTime, FieldIndexing.Analyzed);
        }
    }
}
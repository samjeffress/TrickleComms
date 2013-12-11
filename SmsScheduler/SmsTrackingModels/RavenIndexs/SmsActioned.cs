using System;
using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace SmsTrackingModels.RavenIndexs
{
    // TODO : Use this one!
    public class SmsActioned : AbstractMultiMapIndexCreationTask<SmsActioned.Result>
    {
        public class Result
        {
            public DateTime ActionTime { get; set; }

            public string Number { get; set; }

            public string Message { get; set; }
        }

        public SmsActioned()
        {
            AddMap<SmsReceivedData>(messages => from message in messages 
                select new Result
                {
                    ActionTime = message.SmsConfirmationData.SentAtUtc,
                    Number = message.SmsData.Mobile,
                    Message = message.SmsData.Mobile
                });

            AddMap<SmsTrackingData>(messages => from message in messages 
                select new Result
                {
                    ActionTime = message.ConfirmationData.SentAtUtc,
                    Number = message.SmsData.Mobile,
                    Message = message.SmsData.Mobile
                });

            Index(a => a.ActionTime, FieldIndexing.Analyzed);
        }
    }
}
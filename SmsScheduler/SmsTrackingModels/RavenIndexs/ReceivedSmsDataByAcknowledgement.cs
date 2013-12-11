using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace SmsTrackingModels.RavenIndexs
{
    public class ReceivedSmsDataByAcknowledgement : AbstractIndexCreationTask<SmsReceivedData>
    {
        public ReceivedSmsDataByAcknowledgement()
        {
            Map = smsReceivedDatas => from receivedData in smsReceivedDatas
                select new
                {
                    CreationDate = receivedData.SmsConfirmationData.SentAtUtc,
                    receivedData.Acknowledge
                };
            Indexes.Add(c => c.SmsConfirmationData.SentAtUtc, FieldIndexing.Default);
            Indexes.Add(c => c.Acknowledge, FieldIndexing.Default);
        }
    }
}
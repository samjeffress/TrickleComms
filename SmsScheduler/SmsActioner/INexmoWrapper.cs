using System;
using ConfigurationModels;
using ConfigurationModels.Providers;
using SmsMessages.CommonData;
using Softwarehuset.NexmoClient;

namespace SmsActioner
{
    public interface INexmoWrapper
    {
        SmsStatus SendSmsMessage(string to, string message);
    }

    public class NexmoWrapper : INexmoWrapper
    {
        private IRavenDocStore DocumentStore { get; set; }

        public SmsStatus SendSmsMessage(string to, string message)
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var nexmoConfiguration = session.Load<NexmoConfiguration>("NexmoConfig");
                if (nexmoConfiguration == null)
                {
                    throw new ArgumentException("Could not find nexmo configuration");
                }

                var client = new NexmoClient(nexmoConfiguration.ApiKey, nexmoConfiguration.Secret, ProtocolType.Https);
                var response = client.SendSms(nexmoConfiguration.From, to, message);
                if (response == null || response.Messages == null || response.Messages.Count == 0)
                    throw new Exception("Didn't get a proper response from nexmo");

                if (response.Messages.Count > 1)
                    throw new Exception("not sure what to do here - need integration testing");
                if (response.Messages[0].Status == 0) // success case
                {
                    decimal cost = 0;
                    decimal.TryParse(response.Messages[0].Messageprice, out cost);
                    return new SmsSent(new SmsConfirmationData(response.Messages[0].MessageId, DateTime.UtcNow, cost));
                }
                throw new Exception("Haven't figured out the other status codes yet...");

            }

        }
    }
}
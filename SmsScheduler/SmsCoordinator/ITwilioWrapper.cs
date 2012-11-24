using System;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions; // Used for EnsureDatabaseExists
using Twilio;

namespace SmsCoordinator
{
    public interface ITwilioWrapper
    {
        SMSMessage SendSmsMessage(string to, string message);
        SMSMessage CheckMessage(string sid);
    }

    public class TwilioWrapper : ITwilioWrapper
    {
        private readonly TwilioRestClient _restClient;

        private IRavenDocStore DocumentStore { get; set; }

        public TwilioWrapper(IRavenDocStore documentStore)
        {
            DocumentStore = documentStore;
            string accountSid;
            string authToken;
            using (var session = DocumentStore.GetStore().OpenSession("TwilioConfiguration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                {
                    throw new ArgumentException("Could not find twilio configuration");
                }
                accountSid = twilioConfiguration.AccountSid;
                authToken = twilioConfiguration.AuthToken;
            }

            _restClient = new TwilioRestClient(accountSid, authToken);
        }

        public SMSMessage SendSmsMessage(string to, string message)
        {
            using (var session = DocumentStore.GetStore().OpenSession("TwilioConfiguration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                {
                    throw new NotImplementedException();
                }
                return _restClient.SendSmsMessage(twilioConfiguration.From, to, message);
            }
            
        }

        public SMSMessage CheckMessage(string sid)
        {
            return _restClient.GetSmsMessage(sid);
        }
    }

    public class RavenDocStore : IRavenDocStore
    {
        private readonly IDocumentStore _documentStore;
        public RavenDocStore()
        {
            _documentStore = new DocumentStore { Url = "http://localhost:8080" };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("TwilioConfiguration");
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }

    public interface IRavenDocStore
    {
        IDocumentStore GetStore();
    }
}
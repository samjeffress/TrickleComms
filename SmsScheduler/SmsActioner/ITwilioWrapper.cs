using System;
using ConfigurationModels;
using Twilio;

namespace SmsActioner
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
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
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
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                {
                    throw new NotImplementedException();
                }
                //_restClient.UpdateAccountName("toby toogood");
                return _restClient.SendSmsMessage(twilioConfiguration.From, to, message);
            }
        }

        public SMSMessage CheckMessage(string sid)
        {
            return _restClient.GetSmsMessage(sid);
        }
    }
}
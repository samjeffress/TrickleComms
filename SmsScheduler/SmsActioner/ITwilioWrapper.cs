using System;
using ConfigurationModels;
using SmsMessages.CommonData;
using Twilio;

namespace SmsActioner
{
    public interface ITwilioWrapper
    {
        SmsStatus SendSmsMessage(string to, string message);
        SmsStatus CheckMessage(string sid);
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

        public SmsStatus SendSmsMessage(string to, string message)
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var twilioConfiguration = session.Load<TwilioConfiguration>("TwilioConfig");
                if (twilioConfiguration == null)
                {
                    throw new NotImplementedException();
                }
                var response = _restClient.SendSmsMessage(twilioConfiguration.From, to, message);
                return ProcessResponse(response);
            }
        }

        public SmsStatus CheckMessage(string sid)
        {
            var response = _restClient.GetSmsMessage(sid);
            return ProcessResponse(response);
        }

        public static SmsStatus ProcessResponse(SMSMessage twilioResponse)
        {
            if ((string.IsNullOrWhiteSpace(twilioResponse.Status) && twilioResponse.RestException != null)
                || twilioResponse.Status.Equals("failed", StringComparison.CurrentCultureIgnoreCase))
            {
                var e = twilioResponse.RestException;
                return new SmsFailed(twilioResponse.Sid, e.Code, e.Message, e.MoreInfo, e.Status);
            }

            if (twilioResponse.Status.Equals("sent", StringComparison.CurrentCultureIgnoreCase))
                return new SmsSent(new SmsConfirmationData(twilioResponse.Sid, twilioResponse.DateSent, twilioResponse.Price));

            if (twilioResponse.Status.Equals("sending", StringComparison.CurrentCultureIgnoreCase))
            {
                return new SmsSending(twilioResponse.Sid);
            }

            if (twilioResponse.Status.Equals("queued", StringComparison.CurrentCultureIgnoreCase))
                return new SmsQueued(twilioResponse.Sid);

            return null;
        }
    }
}

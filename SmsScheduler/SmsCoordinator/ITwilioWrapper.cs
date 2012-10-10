using Twilio;

namespace SmsCoordinator
{
    public interface ITwilioWrapper
    {
        SMSMessage SendSmsMessage(string from, string to, string message);
        SMSMessage CheckMessage(string sid);
    }

    public class TwilioWrapper : ITwilioWrapper
    {
        private readonly TwilioRestClient _restClient;
        public TwilioWrapper()
        {
            _restClient = new TwilioRestClient("accountSid", "authToken");
        }

        public SMSMessage SendSmsMessage(string from, string to, string message)
        {
            return _restClient.SendSmsMessage(from, to, message);
        }

        public SMSMessage CheckMessage(string sid)
        {
            return _restClient.GetSmsMessage(sid);
        }
    }
}
using System;
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
        public SMSMessage SendSmsMessage(string from, string to, string message)
        {
            //var twilioRestClient = new TwilioRestClient("accountSid", "authToken");
            ////twilioRestClient.getsm
            throw new NotImplementedException();
        }

        public SMSMessage CheckMessage(string sid)
        {
            throw new NotImplementedException();
        }
    }
}
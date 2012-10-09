using SmsMessages.Commands;
using Twilio;

namespace SmsCoordinator
{
    public interface ISmsService
    {
        string Send(SendOneMessageNow messageToSend);
    }

    public class SmsService : ISmsService
    {
        public ITwilioWrapper TwilioWrapper { get; set; }

        public string Send(SendOneMessageNow messageToSend)
        {
            var sendSmsMessage = TwilioWrapper.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message);
            return sendSmsMessage.Sid;
        }
    }

    
    public interface ITwilioWrapper
    {
        SMSMessage SendSmsMessage(string from, string to, string message);
    }
}

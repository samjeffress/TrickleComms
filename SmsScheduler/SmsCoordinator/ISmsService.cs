using System;
using SmsMessages.CommonData;
using SmsMessages.MessageSending;
using Twilio;

namespace SmsCoordinator
{
    public interface ISmsService
    {
        /// <summary>
        /// Send the SMS message through a provider
        /// </summary>
        /// <param name="messageToSend">Phone number and message to send to contact</param>
        /// <returns>Status of the SMS message, including SId from the provider</returns>
        SmsStatus Send(SendOneMessageNow messageToSend);

        SmsStatus CheckStatus(string sid);
    }

    public class SmsService : ISmsService
    {
        public ITwilioWrapper TwilioWrapper { get; set; }

        public SmsStatus Send(SendOneMessageNow messageToSend)
        {
            var createdSmsMessage = TwilioWrapper.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message);
            return ProcessSms(createdSmsMessage);
        }

        public SmsStatus CheckStatus(string sid)
        {
            var checkMessage = TwilioWrapper.CheckMessage(sid);
            return ProcessSms(checkMessage);
        }

        private SmsStatus ProcessSms(SMSMessage createdSmsMessage)
        {
            if (createdSmsMessage.Status.Equals("sent", StringComparison.CurrentCultureIgnoreCase))
                return new SmsSent(new SmsConfirmationData(createdSmsMessage.Sid, createdSmsMessage.DateSent, createdSmsMessage.Price)); 

            if (createdSmsMessage.Status.Equals("sending", StringComparison.CurrentCultureIgnoreCase))
            {
                return new SmsSending(createdSmsMessage.Sid);
            }

            if (createdSmsMessage.Status.Equals("failed", StringComparison.CurrentCultureIgnoreCase))
            {
                var e = createdSmsMessage.RestException;
                return new SmsFailed(createdSmsMessage.Sid, e.Code, e.Message, e.MoreInfo, e.Status);
            }

            if (createdSmsMessage.Status.Equals("queued", StringComparison.CurrentCultureIgnoreCase))
                return new SmsQueued(createdSmsMessage.Sid);

            return null;
        }
    }
}

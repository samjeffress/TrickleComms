using System;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;
using Twilio;

namespace SmsActioner
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

        public ISmsTechWrapper SmsTechWrapper { get; set; }

        /// <summary>
        /// This method will never get success / fail of message delivery - just that it is valid, and how much it will cost
        /// </summary>
        /// <param name="messageToSend"></param>
        /// <returns></returns>
        public SmsStatus Send(SendOneMessageNow messageToSend)
        {
            var createdSmsMessage = SmsTechWrapper.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message);
            if (createdSmsMessage.Error.Code.Equals("LEDGER_ERROR"))
                throw new AccountOutOfMoneyException("Could not send message - account is currently out of money");
            if (createdSmsMessage.Error.Code.Equals("RECIPIENTS_ERROR"))
                return new SmsFailed(createdSmsMessage.MessageId.ToString(), createdSmsMessage.Error.Code, createdSmsMessage.Error.Description);
            return new SmsSending(createdSmsMessage.MessageId.ToString(), Convert.ToDecimal(createdSmsMessage.Cost));
        }

        public SmsStatus CheckStatus(string sid)
        {
            var checkMessage = TwilioWrapper.CheckMessage(sid);
            return ProcessSms(checkMessage);
        }

        private SmsStatus ProcessSms(SMSMessage createdSmsMessage)
        {
            if ((string.IsNullOrWhiteSpace(createdSmsMessage.Status) && createdSmsMessage.RestException != null)
                || createdSmsMessage.Status.Equals("failed", StringComparison.CurrentCultureIgnoreCase))
            {
                var e = createdSmsMessage.RestException;
                return new SmsFailed(createdSmsMessage.Sid, e.Code, e.Message);
            } 
            
            if (createdSmsMessage.Status.Equals("sent", StringComparison.CurrentCultureIgnoreCase))
                return new SmsSent(new SmsConfirmationData(createdSmsMessage.Sid, createdSmsMessage.DateSent, createdSmsMessage.Price)); 

            if (createdSmsMessage.Status.Equals("sending", StringComparison.CurrentCultureIgnoreCase))
            {
                return new SmsSending(createdSmsMessage.Sid, 10.10m);
            }

            

            if (createdSmsMessage.Status.Equals("queued", StringComparison.CurrentCultureIgnoreCase))
                return new SmsQueued(createdSmsMessage.Sid);

            return null;
        }
    }
}

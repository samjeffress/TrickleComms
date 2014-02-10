using System;
using System.Linq;
using SmsMessages.CommonData;
using SmsMessages.MessageSending.Commands;

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
        public ISmsTechWrapper SmsTechWrapper { get; set; }

        /// <summary>
        /// This method will never get success / fail of message delivery - just that it is valid, and how much it will cost
        /// </summary>
        /// <param name="messageToSend"></param>
        /// <returns></returns>
        public SmsStatus Send(SendOneMessageNow messageToSend)
        {
            var createdSmsMessage = SmsTechWrapper.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message);
            if (createdSmsMessage.Error.Code.Equals("AUTH_FAILED"))
                throw new SmsTechAuthenticationFailed(createdSmsMessage.Error.Description);
            if (createdSmsMessage.Error.Code.Equals("LEDGER_ERROR"))
                throw new AccountOutOfMoneyException("Could not send message - account is currently out of money");
            if (createdSmsMessage.Error.Code.Equals("RECIPIENTS_ERROR"))
                return new SmsFailed(createdSmsMessage.MessageId.ToString(), createdSmsMessage.Error.Code, createdSmsMessage.Error.Description);
            if (createdSmsMessage.Error.Code.Equals("SUCCESS"))
                return new SmsSending(createdSmsMessage.MessageId.ToString(), Convert.ToDecimal(createdSmsMessage.Cost));
            throw new ArgumentException("Error code expected");
        }

        public SmsStatus CheckStatus(string sid)
        {
            var smsSentResponse = SmsTechWrapper.CheckMessage(sid);
            var recipientForSms = smsSentResponse.Recipients.First();
            if (recipientForSms.DeliveryStatus.Equals("hard-bounce", StringComparison.CurrentCultureIgnoreCase))
                return new SmsFailed(sid, recipientForSms.DeliveryStatus, "The number is invalid or disconnected.");
            if (recipientForSms.DeliveryStatus.Equals("soft-bounce", StringComparison.CurrentCultureIgnoreCase))
                return new SmsFailed(sid, recipientForSms.DeliveryStatus, "The message timed out after 72 hrs, either the recipient was out of range, their phone was off for longer than 72 hrs or the message was unable to be delivered due to a network outage or other connectivity issue.");
            if (recipientForSms.DeliveryStatus.Equals("pending", StringComparison.CurrentCultureIgnoreCase))
                return new SmsQueued(sid);
            if (recipientForSms.DeliveryStatus.Equals("delivered", StringComparison.CurrentCultureIgnoreCase))
                return new SmsSent(sid, smsSentResponse.Message.SendAt);
            throw new ArgumentException("Unexpected delivery status " + recipientForSms.DeliveryStatus);
        }
    }
}

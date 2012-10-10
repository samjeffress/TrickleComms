using System;
using System.Threading;
using SmsMessages.Commands;
using Twilio;

namespace SmsCoordinator
{
    public interface ISmsService
    {
        /// <summary>
        /// Send the SMS message through a provider
        /// </summary>
        /// <param name="messageToSend">Phone number and message to send to contact</param>
        /// <returns>Receipt Id from provider</returns>
        string Send(SendOneMessageNow messageToSend);
    }

    public class SmsService : ISmsService
    {
        public ITwilioWrapper TwilioWrapper { get; set; }

        private int _waitingForSendingTries = 0;

        public string Send(SendOneMessageNow messageToSend)
        {
            var createdSmsMessage = TwilioWrapper.SendSmsMessage("defaultFrom", messageToSend.SmsData.Mobile, messageToSend.SmsData.Message);
            return ProcessSms(createdSmsMessage);
        }

        private string ProcessSms(SMSMessage createdSmsMessage)
        {
            if (createdSmsMessage.Status.Equals("sent", StringComparison.CurrentCultureIgnoreCase))
                return createdSmsMessage.Sid;

            if (createdSmsMessage.Status.Equals("sending", StringComparison.CurrentCultureIgnoreCase))
            {
                if (_waitingForSendingTries > 4)
                    throw new ArgumentException("Waited too long for message to send - retry later");
                _waitingForSendingTries++;
                var updatedMessage = TwilioWrapper.CheckMessage(createdSmsMessage.Sid);
                Thread.Sleep(1000);
                return ProcessSms(updatedMessage);
            }

            if (createdSmsMessage.Status.Equals("failed", StringComparison.CurrentCultureIgnoreCase))
            {
                RaiseTwilioException(createdSmsMessage);
            }

            if (createdSmsMessage.Status.Equals("queued", StringComparison.CurrentCultureIgnoreCase))
                throw new NotImplementedException("Not sure what to do with a queued message");

            throw new NotImplementedException();
        }

        private void RaiseTwilioException(SMSMessage createdSmsMessage)
        {
            if (createdSmsMessage.RestException != null)
            {
                string exceptionMessage = String.Format("Rest Exception: {0} (Http Status {3}). /n Message: {1}/n More Info At {2}",
                                                        createdSmsMessage.RestException.Code, 
                                                        createdSmsMessage.RestException.Message,
                                                        createdSmsMessage.RestException.MoreInfo, 
                                                        createdSmsMessage.RestException.Status);
                throw new Exception(exceptionMessage);
            }
            throw new Exception("Message sending failed");
        }
    }
}

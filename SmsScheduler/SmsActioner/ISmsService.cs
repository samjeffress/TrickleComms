using System;
using ConfigurationModels;
using Raven.Client.Document;
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
        public INexmoWrapper NexmoWrapper { get; set; }
        public IRavenDocStore RavenDocStore { get; set; }

        public SmsStatus Send(SendOneMessageNow messageToSend)
        {
            using (var session = RavenDocStore.GetStore().OpenSession(RavenDocStore.ConfigurationDatabaseName()))
            {
                var smsProvider = session.Load<SmsProviderConfiguration>("SmsProviderConfiguration");
                if (smsProvider == null)
                    throw new Exception("No SMS provider selected");
                switch (smsProvider.SmsProvider)
                {
                    case SmsProvider.Nexmo:
                        return NexmoWrapper.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message);
                    case SmsProvider.Twilio:
                        return ProcessTwilioResponse(TwilioWrapper.SendSmsMessage(messageToSend.SmsData.Mobile, messageToSend.SmsData.Message));
                }
                throw new Exception("SMS Provder delivery not implemented for " + smsProvider.SmsProvider.ToString());
            }
        }

        public SmsStatus CheckStatus(string sid)
        {
            var checkMessage = TwilioWrapper.CheckMessage(sid);
            return ProcessTwilioResponse(checkMessage);
        }

        private SmsStatus ProcessTwilioResponse(SMSMessage createdSmsMessage)
        {
            if ((string.IsNullOrWhiteSpace(createdSmsMessage.Status) && createdSmsMessage.RestException != null)
                || createdSmsMessage.Status.Equals("failed", StringComparison.CurrentCultureIgnoreCase))
            {
                var e = createdSmsMessage.RestException;
                return new SmsFailed(createdSmsMessage.Sid, e.Code, e.Message, e.MoreInfo, e.Status);
            } 
            
            if (createdSmsMessage.Status.Equals("sent", StringComparison.CurrentCultureIgnoreCase))
                return new SmsSent(new SmsConfirmationData(createdSmsMessage.Sid, createdSmsMessage.DateSent, createdSmsMessage.Price)); 

            if (createdSmsMessage.Status.Equals("sending", StringComparison.CurrentCultureIgnoreCase))
            {
                return new SmsSending(createdSmsMessage.Sid);
            }

            

            if (createdSmsMessage.Status.Equals("queued", StringComparison.CurrentCultureIgnoreCase))
                return new SmsQueued(createdSmsMessage.Sid);

            return null;
        }
    }
}

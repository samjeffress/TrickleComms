using System;
using ConfigurationModels;
using TransmitSms.Models.Sms;

namespace SmsActioner
{
    public interface ISmsTechWrapper
    {
        SendSmsResponse SendSmsMessage(string to, string message);
        SmsSentResponse CheckMessage(string sid);
    }

    public class SmsTechWrapper : ISmsTechWrapper
    {
        private IRavenDocStore DocumentStore { get; set; }

        private TransmitSms.TransmitSmsWrapper TransmitSmsClient { get; set; }

        public SmsTechWrapper(IRavenDocStore documentStore)
        {
            DocumentStore = documentStore;
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var smsTechConfiguration = session.Load<SmsTechConfiguration>("SmsTechConfig");
                if (smsTechConfiguration == null)
                {
                    throw new ArgumentException("Could not find sms tech configuration");
                }
                TransmitSmsClient = new TransmitSms.TransmitSmsWrapper(smsTechConfiguration.ApiKey, smsTechConfiguration.ApiSecret);
            }
        }

        public SendSmsResponse SendSmsMessage(string to, string message)
        {
            throw new System.NotImplementedException();
        }

        public SmsSentResponse CheckMessage(string sid)
        {
            throw new System.NotImplementedException();
        }
    }
}
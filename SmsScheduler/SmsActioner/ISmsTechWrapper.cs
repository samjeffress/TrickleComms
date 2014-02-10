using System;
using ConfigurationModels;
using TransmitSms.Models;
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
                    throw new ArgumentException("Could not find sms tech configuration");
                TransmitSmsClient = new TransmitSms.TransmitSmsWrapper(smsTechConfiguration.ApiKey, smsTechConfiguration.ApiSecret, @"https://api.transmitsms.com");
                /* - ALL FOR CHECKING TRANSMIT SMS NUGET PACKAGE 
                BaseUrl = @"https://api.transmitsms.com/";
                AuthHeader = string.Format("Basic {0}", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", smsTechConfiguration.ApiKey, smsTechConfiguration.ApiSecret))));
                RestClient = new RestClient(BaseUrl);
                RestClient.AddDefaultHeader("Authorization", AuthHeader);
                RestClient.AddDefaultHeader("Accept", "application/json");
                */
            }
        }

        public SendSmsResponse SendSmsMessage(string to, string message)
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var smsTechConfiguration = session.Load<SmsTechConfiguration>("SmsTechConfig");
                if (smsTechConfiguration == null)
                    throw new NotImplementedException();
                /* - ALL FOR CHECKING TRANSMIT SMS NUGET PACKAGE 
                var request = new RestRequest("send-sms.json", Method.POST);
                request.AddParameter("message", message, ParameterType.GetOrPost);
                request.AddParameter("to", to, ParameterType.GetOrPost);
                request.AddParameter("from", smsTechConfiguration.From, ParameterType.GetOrPost);
                //request.AddParameter("dlr_callback", (object)dlrCallback, ParameterType.GetOrPost);
                //request.AddParameter("reply_callback", (object)replyCallback, ParameterType.GetOrPost);
                request.AddParameter("validity", 0, ParameterType.GetOrPost);
                //var response = RestClient.Execute(request);
                */

                return TransmitSmsClient.SendSms(message, new[]{to}, smsTechConfiguration.From, null, null, null, null, 0);
            }
        }

        public SmsSentResponse CheckMessage(string sid)
        {
            return TransmitSmsClient.GetSmsSent(Convert.ToInt32(sid), OptoutsIncludeOptions.Include, 1, 1);
        }
    }
}
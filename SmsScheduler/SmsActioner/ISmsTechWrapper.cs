using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;
using ConfigurationModels;
using Newtonsoft.Json;
using RestSharp;
using TransmitSms.Helpers;
using TransmitSms.Models;
using TransmitSms.Models.Recipients;
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

        private RestClient RestClient { get; set; }

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
                /* - ALL FOR CHECKING TRANSMIT SMS NUGET PACKAGE */
                var baseUrl = @"https://api.transmitsms.com/";
                var authHeader = string.Format("Basic {0}", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", smsTechConfiguration.ApiKey, smsTechConfiguration.ApiSecret))));
                RestClient = new RestClient(baseUrl);
                RestClient.AddDefaultHeader("Authorization", authHeader);
                RestClient.AddDefaultHeader("Accept", "application/json");
                
            }
        }

        public SendSmsResponse SendSmsMessage(string to, string message)
        {
            using (var session = DocumentStore.GetStore().OpenSession("Configuration"))
            {
                var smsTechConfiguration = session.Load<SmsTechConfiguration>("SmsTechConfig");
                if (smsTechConfiguration == null)
                    throw new NotImplementedException();

                return TransmitSmsClient.SendSms(message, new[]{to}, smsTechConfiguration.From, null, null, null, null, 0);
            }
        }

        public SmsSentResponse CheckMessage(string sid)
        {
            RestRequest request = new RestRequest("get-sms-sent.json", Method.POST);
            request.AddParameter("message_id", (object)sid, ParameterType.GetOrPost);
            request.AddParameter("optout", (object)EnumUtility.GetEnumDescription((Enum)OptoutsIncludeOptions.Include), ParameterType.GetOrPost);
            request.AddParameter("page", (object)1, ParameterType.GetOrPost);
            request.AddParameter("max", (object)1, ParameterType.GetOrPost);
            var restResponse = RestClient.Execute(request);

            if (restResponse.ErrorException != null)
                throw new Exception(restResponse.ErrorException.Message, restResponse.ErrorException);
            else
            {
                var j =  JsonConvert.DeserializeObject<SmsSentResponse>(restResponse.Content);
                return j;
            }
            //return this._serviceUtility.Execute<SmsSentResponse>(request);

            /* - ALL FOR CHECKING TRANSMIT SMS NUGET PACKAGE */
            //var request = new RestRequest("send-sms.json", Method.POST);
            //request.AddParameter("message", message, ParameterType.GetOrPost);
            //request.AddParameter("to", to, ParameterType.GetOrPost);
            //request.AddParameter("from", smsTechConfiguration.From, ParameterType.GetOrPost);
            ////request.AddParameter("dlr_callback", (object)dlrCallback, ParameterType.GetOrPost);
            ////request.AddParameter("reply_callback", (object)replyCallback, ParameterType.GetOrPost);
            //request.AddParameter("validity", 0, ParameterType.GetOrPost);
            ////var response = RestClient.Execute(request);

            //return TransmitSmsClient.GetSmsSent(Convert.ToInt32(sid), OptoutsIncludeOptions.Include, 1, 1);
        }
    }

    [DataContract]
    public class SmsSentResponse : ResponseBase
    {
        [DataMember(Name = "page")]
        public PageModel Page { get; set; }

        [DataMember(Name = "total")]
        public int Total { get; set; }

        [DataMember(Name = "message")]
        public SmsResponseBase Message { get; set; }

        [DataMember(Name = "recipients")]
        public IEnumerable<RecipientForSms> Recipients { get; set; }
    }
}
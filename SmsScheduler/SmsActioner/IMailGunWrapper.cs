using System;
using System.Collections.Generic;
using ConfigurationModels;
using Newtonsoft.Json;
using RestSharp;
using SmsActioner.InternalMessages.Commands;
using SmsMessages;

namespace SmsActioner
{
    public interface IMailGunWrapper
    {
        string SendEmail(SendEmail message);
        EmailStatus CheckStatus(string emailId);
    }

    public class MailGunWrapper : IMailGunWrapper
    {
        public IRavenDocStore DocumentStore { get; set; }

        public string SendEmail(SendEmail message)
        {
            using (var session = DocumentStore.GetStore().OpenSession(DocumentStore.ConfigurationDatabaseName()))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                {
                    throw new NotImplementedException();
                }

                var baseMessage = message.BaseRequest;
                var client = new RestClient
                {
                    BaseUrl = new Uri("https://api.mailgun.net/v2"),
                    Authenticator = new HttpBasicAuthenticator("api", mailgunConfiguration.ApiKey)
                };
                RestRequest request = new RestRequest();
                request.AddParameter("domain", "tricklesms.com", ParameterType.UrlSegment);
                request.Resource = "{domain}/messages";
                request.AddParameter("from", baseMessage.FromDisplayName + " <" + baseMessage.FromAddress + ">");
                request.AddParameter("to", baseMessage.ToAddress);
                request.AddParameter("subject", baseMessage.Subject);
                request.AddParameter("text", baseMessage.BodyText);
                request.AddParameter("html", baseMessage.BodyHtml);
                request.AddParameter("h:Reply-To", baseMessage.ReplyToAddress);
                request.Method = Method.POST;
                var response = client.Execute<dynamic>(request);
                var content = SimpleJson.DeserializeObject<Dictionary<string,string>>(response.Content);
                string id = content["id"];
                //var responseContent = JsonConvert.DeserializeObject<dynamic>(response.Content);
                //var innercontent = SimpleJson.DeserializeObject<dynamic>(content);
                //var id = innercontent.id;
                //var id = response.Data.id;
                id = id.Replace('<', ' ');
                id = id.Replace('>', ' ');
                id = id.Trim();
                return id;
            }
        }

        public EmailStatus CheckStatus(string emailId)
        {
            using (var session = DocumentStore.GetStore().OpenSession(DocumentStore.ConfigurationDatabaseName()))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                {
                    throw new NotImplementedException();
                }

                var client = new RestClient
                {
                    BaseUrl = new Uri("https://api.mailgun.net/v2"),
                    Authenticator = new HttpBasicAuthenticator("api", mailgunConfiguration.ApiKey)
                };
                var request = new RestRequest();
                request.AddParameter("domain", "tricklesms.com", ParameterType.UrlSegment);
                request.Resource = "{domain}/events";
                // begin doesn't matter because we use message-id, but mailgun needs this field
                request.AddParameter("begin", "Thu, 13 Oct 2011 18:02:00 GMT");
                request.AddParameter("ascending", "yes");
                request.AddParameter("limit", 25);
                request.AddParameter("pretty", "yes");
                request.AddParameter("message-id", emailId);
                var response = client.Execute<dynamic>(request);

                var responseContent = JsonConvert.DeserializeObject<dynamic>(response.Content);
                var itemCount = responseContent.items.Count;
                var eventStatus = responseContent.items[itemCount-1]["event"].Value;
                EmailStatus result;
                if (Enum.TryParse(eventStatus, true, out result))
                    return result;
                throw new NotImplementedException();
            }
        }
    }
}
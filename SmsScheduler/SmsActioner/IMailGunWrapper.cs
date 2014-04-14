using System;
using ConfigurationModels;
using Newtonsoft.Json;
using RestSharp;
using SmsActioner.InternalMessages.Commands;

namespace SmsActioner
{
    public interface IMailGunWrapper
    {
        string SendEmail(SendEmail message);
    }

    public class MailGunWrapper : IMailGunWrapper
    {
        private IRavenDocStore DocumentStore { get; set; }

        public string SendEmail(SendEmail message)
        {
            using (var session = DocumentStore.GetStore().OpenSession(DocumentStore.DatabaseName()))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null)
                {
                    throw new NotImplementedException();
                }

                var baseMessage = message.BaseRequest;
                var client = new RestClient
                {
                    BaseUrl = "https://api.mailgun.net/v2",
                    Authenticator = new HttpBasicAuthenticator("api",
                                                               mailgunConfiguration.ApiKey)
                };
                RestRequest request = new RestRequest();
                request.AddParameter("domain",
                                     "tricklesms.com", ParameterType.UrlSegment);
                request.Resource = "{domain}/messages";
                request.AddParameter("from", baseMessage.FromDisplayName + " <" + baseMessage.FromAddress + ">");
                request.AddParameter("to", baseMessage.ToAddress);
                request.AddParameter("subject", baseMessage.Subject);
                request.AddParameter("text", baseMessage.BodyText);
                request.AddParameter("html", baseMessage.BodyHtml);
                request.AddParameter("h:Reply-To", baseMessage.ReplyToAddress);
                request.Method = Method.POST;
                var response = client.Execute(request);
                var responseContent = JsonConvert.DeserializeObject<dynamic>(response.Content);
                var id = responseContent.id as string;
                id = id.Replace('<', ' ');
                id = id.Replace('>', ' ');
                id = id.Trim();
                return id;
            }
        }
    }
}
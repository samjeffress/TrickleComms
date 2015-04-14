using System;
using System.Linq;
using System.Net.Mail;
using ConfigurationModels;
using RestSharp;

namespace SmsCoordinator.Email
{
    public interface IMailActioner
    {
        void Send(MailgunConfiguration configuration, MailMessage message);
    }

    public class MailActioner : IMailActioner
    {
        public void Send(MailgunConfiguration configuration, MailMessage message)
        {
            var client = new RestClient
            {
                BaseUrl = new Uri("https://api.mailgun.net/v2"),
                Authenticator = new HttpBasicAuthenticator("api", configuration.ApiKey)
            };
            RestRequest request = new RestRequest();
            request.AddParameter("domain", configuration.DomainName, ParameterType.UrlSegment);
            request.Resource = "{domain}/messages";
            request.AddParameter("from", message.From.DisplayName + " <" + message.From.Address + ">");
            request.AddParameter("to", string.Join(",", message.To.Select(t => t.Address).ToArray()));
            request.AddParameter("subject", message.Subject);
//            request.AddParameter("text", message.Body);
            request.AddParameter("html", message.Body);
            request.AddParameter("h:Reply-To", message.ReplyToList.Select(r => r.DisplayName + "<" + r.Address + ">").FirstOrDefault());
            request.Method = Method.POST;
            var response = client.Execute<dynamic>(request);
            Console.Write(response);
        }
    }
}
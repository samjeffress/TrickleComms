using System;
using ConfigurationModels;
using ConfigurationModels.Providers;
using MailChimp;
using SmsActioner.InternalMessages.Commands;

namespace SmsActioner
{
    public interface IMandrillWrapper
    {
        string SendEmail(SendEmail emailMessage);
    }

    public class MandrillWrapper : IMandrillWrapper
    {
        public IRavenDocStore DocumentStore { get; set; }

        public string SendEmail(SendEmail emailMessage)
        {
            using (var session = DocumentStore.GetStore().OpenSession(DocumentStore.ConfigurationDatabaseName()))
            {
                var mandrillConfig = session.Load<MailgunConfiguration>("MandrillConfig");
                if (mandrillConfig == null)
                {
                    throw new NotImplementedException();
                }

                var api = new MandrillApi(mandrillConfig.ApiKey);
                var message = new MailChimp.Types.Mandrill.Messages.Message();

                message.Html = emailMessage.BaseRequest.BodyHtml;
                message.Text = emailMessage.BaseRequest.BodyText;
                message.To = new[]{new MailChimp.Types.Mandrill.Messages.Recipient(emailMessage.BaseRequest.ToAddress, emailMessage.BaseRequest.ToAddress), }; //new List<EmailAddress>() {new EmailAddress(emailMessage.BaseRequest.ToAddress)};
                message.Subject = emailMessage.BaseRequest.Subject;
                message.FromEmail = emailMessage.BaseRequest.FromAddress;
                message.FromName = emailMessage.BaseRequest.FromDisplayName;
                var result = api.Send(message);
                if (result == null || result.Count == 0)
                    throw new Exception("Email should be getting something back....");
                if (result[0].Status == MailChimp.Types.Mandrill.Messages.Status.Invalid || result[0].Status == MailChimp.Types.Mandrill.Messages.Status.Bounced || result[0].Status == MailChimp.Types.Mandrill.Messages.Status.Rejected || result[0].Status == MailChimp.Types.Mandrill.Messages.Status.SoftBounced)
                    throw new Exception("Some exception because email failed");
                return result[0].ID;
            }
        }
    }
}
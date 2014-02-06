using System;
using System.Net.Mail;
using ConfigurationModels;
using Typesafe.Mailgun;

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
            var mailgunClient = new MailgunClient(configuration.DomainName, configuration.ApiKey);
            var commandResult = mailgunClient.SendMail(message);
            var s = commandResult.Message;
            Console.Write(s);
        }
    }
}
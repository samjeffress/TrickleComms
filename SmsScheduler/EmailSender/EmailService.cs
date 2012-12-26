using System;
using System.Net.Mail;
using ConfigurationModels;
using NServiceBus;
using SmsMessages.MessageSending.Events;
using SmsTrackingMessages.Messages;

namespace EmailSender
{
    public class EmailService : 
        IHandleMessages<MessageSent>,
        IHandleMessages<MessageFailedSending>,
        IHandleMessages<CoordinatorCompleteEmail>
    {
        public IRavenDocStore RavenDocStore { get; set; }
        public IMailActioner MailActioner { get; set; }

        public void Handle(MessageSent message)
        {
            if (string.IsNullOrWhiteSpace(message.ConfirmationEmailAddress))
                return;
            using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null || string.IsNullOrWhiteSpace(mailgunConfiguration.DefaultFrom))
                    throw new ArgumentException("Could not find the default 'From' sender.");
                var subject = "Message to " + message.SmsData.Mobile + " sent.";

                var body = string.Format("Message '{0}' sent to number {1}. \r\nCost: ${2} \r\nSent (UTC): {3}", message.SmsData.Message, message.SmsData.Mobile, message.ConfirmationData.Price, message.ConfirmationData.SentAtUtc.ToString());
                var mailMessage = new MailMessage(mailgunConfiguration.DefaultFrom, message.ConfirmationEmailAddress, subject, body);
                MailActioner.Send(mailMessage);
            }
        }

        public void Handle(MessageFailedSending message)
        {
            if (string.IsNullOrWhiteSpace(message.ConfirmationEmailAddress))
                return;
            using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
            {
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null || string.IsNullOrWhiteSpace(mailgunConfiguration.DefaultFrom))
                    throw new ArgumentException("Could not find the default 'From' sender.");
                var subject = "Message to " + message.SmsData.Mobile + " was not sent.";

                var body = string.Format("Message '{0}' failed sending to number {1}. \r\nFailure Reason: {2} \r\n<a href src='{3}'>More Information</a>", message.SmsData.Message, message.SmsData.Mobile, message.SmsFailed.Message, message.SmsFailed.MoreInfo);
                var mailMessage = new MailMessage(mailgunConfiguration.DefaultFrom, message.ConfirmationEmailAddress, subject, body);
                MailActioner.Send(mailMessage);
            }
        }

        public void Handle(CoordinatorCompleteEmail message)
        {
            throw new System.NotImplementedException();
        }
    }
}

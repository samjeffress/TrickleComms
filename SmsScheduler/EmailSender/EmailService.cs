using System;
using System.Linq;
using System.Net.Mail;
using System.Text;
using ConfigurationModels;
using NServiceBus;
using SmsMessages.Coordinator.Events;
using SmsMessages.MessageSending.Events;
using SmsTrackingMessages.Messages;

namespace EmailSender
{
    public class EmailService : 
        IHandleMessages<MessageSent>,
        IHandleMessages<MessageFailedSending>,
        IHandleMessages<CoordinatorCompleteEmail>,
        IHandleMessages<CoordinatorCreated>
    {
        public IRavenDocStore RavenDocStore { get; set; }
        public IMailActioner MailActioner { get; set; }
        public IDateTimeOlsenFromUtcMapping DateTimeOlsenFromUtcMapping { get; set; }

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
                MailActioner.Send(mailgunConfiguration, mailMessage);
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
                MailActioner.Send(mailgunConfiguration, mailMessage);
            }
        }

        public void Handle(CoordinatorCompleteEmail message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
            {
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                if (message.EmailAddresses.Count == 0 && (emailDefaultNotification == null || emailDefaultNotification.EmailAddresses.Count == 0))
                    return;

                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null || string.IsNullOrWhiteSpace(mailgunConfiguration.DefaultFrom))
                    throw new ArgumentException("Could not find the default 'From' sender.");
                var subject = "Coordinator " + message.Topic + "(" + message.CoordinatorId + ") complete.";

                var finishTimeUserZone = DateTimeOlsenFromUtcMapping.DateTimeUtcToLocalWithOlsenZone(message.FinishTimeUtc, message.UserOlsenTimeZone);

                var body = EmailTemplateResolver.GetEmailBody(@"Templates\CoordinatorFinished.cshtml", new
                {
                    message.CoordinatorId,
                    FinishTimeUserZone = finishTimeUserZone,
                    UserTimeZone = message.UserOlsenTimeZone,
                    MessageCount = message.SendingData.SuccessfulMessages.Count + message.SendingData.UnsuccessfulMessageses.Count,
                    SuccessfulMessageCount = message.SendingData.SuccessfulMessages.Count,
                    UnsuccessfulMessageCount = message.SendingData.UnsuccessfulMessageses.Count,
                    TotalCost = message.SendingData.SuccessfulMessages.Sum(m => m.Cost),
                    Topic = message.Topic
                });

                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailgunConfiguration.DefaultFrom);
                mailMessage.Body = body;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = subject;
                foreach (var emailAddress in message.EmailAddresses)
                {
                    mailMessage.To.Add(emailAddress);
                }
                if (emailDefaultNotification != null)
                    emailDefaultNotification.EmailAddresses.ForEach(e => mailMessage.To.Add(e));
                MailActioner.Send(mailgunConfiguration, mailMessage);
            }
        }

        public void Handle(CoordinatorCreated message)
        {
            using (var session = RavenDocStore.GetStore().OpenSession("Configuration"))
            {
                var emailDefaultNotification = session.Load<EmailDefaultNotification>("EmailDefaultConfig");
                if (message.ConfirmationEmailAddresses.Count == 0 && (emailDefaultNotification == null || emailDefaultNotification.EmailAddresses.Count == 0))
                    return;
                
                var mailgunConfiguration = session.Load<MailgunConfiguration>("MailgunConfig");
                if (mailgunConfiguration == null || string.IsNullOrWhiteSpace(mailgunConfiguration.DefaultFrom))
                    throw new ArgumentException("Could not find the default 'From' sender.");
                var subject = "Coordinator " + message.MetaData.Topic + " (" + message.CoordinatorId + ") created.";

                var creationDateUserZone = DateTimeOlsenFromUtcMapping.DateTimeUtcToLocalWithOlsenZone(message.CreationDateUtc, message.UserOlsenTimeZone);

                var startTimeUserZone =
                    DateTimeOlsenFromUtcMapping.DateTimeUtcToLocalWithOlsenZone(
                        message.ScheduledMessages.Select(s => s.ScheduledTimeUtc).Min(), message.UserOlsenTimeZone);

                var endTimeUserZone =
                    DateTimeOlsenFromUtcMapping.DateTimeUtcToLocalWithOlsenZone(
                        message.ScheduledMessages.Select(s => s.ScheduledTimeUtc).Max(), message.UserOlsenTimeZone);

                var body = EmailTemplateResolver.GetEmailBody(@"Templates\CoordinatorCreated.cshtml", new
                    {
                        message.CoordinatorId,
                        CreationDateUserZone = creationDateUserZone,
                        MessageCount = message.ScheduledMessages.Count,
                        StartTimeUserZone = startTimeUserZone,
                        EndTimeUserZone = endTimeUserZone,
                        UserTimeZone = message.UserOlsenTimeZone,
                        message.MetaData.Topic
                    });

                var mailMessage = new MailMessage();
                mailMessage.From = new MailAddress(mailgunConfiguration.DefaultFrom); 
                mailMessage.Body = body;
                mailMessage.BodyEncoding = Encoding.UTF8;
                mailMessage.IsBodyHtml = true;
                mailMessage.Subject = subject;

                foreach (var emailAddress in message.ConfirmationEmailAddresses)
                {
                    mailMessage.To.Add(emailAddress);
                }

                if (emailDefaultNotification != null)
                    emailDefaultNotification.EmailAddresses.ForEach(e => mailMessage.To.Add(e));
                MailActioner.Send(mailgunConfiguration, mailMessage);
            }
        }
    }


}

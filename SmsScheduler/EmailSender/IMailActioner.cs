using System.Net.Mail;

namespace EmailSender
{
    public interface IMailActioner
    {
        void Send(MailMessage message);
    }

    public class MailActioner : IMailActioner
    {
        public void Send(MailMessage message)
        {
            throw new System.NotImplementedException();
        }
    }
}
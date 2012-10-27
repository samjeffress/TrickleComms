namespace SmsWeb.Models
{
    public class SendNowModel
    {
        public string Number { get; set; }

        public string MessageBody { get; set; }

        public string ConfirmationEmail { get; set; }
    }
}
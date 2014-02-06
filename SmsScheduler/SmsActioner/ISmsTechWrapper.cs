using TransmitSms.Models.Sms;

namespace SmsActioner
{
    public interface ISmsTechWrapper
    {
        SendSmsResponse SendSmsMessage(string to, string message);
        SmsSentResponse CheckMessage(string sid);
    }
}
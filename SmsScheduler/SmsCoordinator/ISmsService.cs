using SmsMessages;

namespace SmsCoordinator
{
    public interface ISmsService
    {
        string Send(SendOneMessageNow messageToSend);
    }
}

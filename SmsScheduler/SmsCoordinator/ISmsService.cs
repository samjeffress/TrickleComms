using System;
using SmsMessages;

namespace SmsCoordinator
{
    public interface ISmsService
    {
        string Send(SendOneMessageNow messageToSend);
    }

    public class SmsService : ISmsService
    {
        public string Send(SendOneMessageNow messageToSend)
        {
            throw new NotImplementedException();
        }
    }
}

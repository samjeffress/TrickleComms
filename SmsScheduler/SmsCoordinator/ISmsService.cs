using System;
using SmsMessages;
using SmsMessages.Commands;

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

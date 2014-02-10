using System;

namespace SmsActioner
{
    public class AccountOutOfMoneyException : Exception
    {
        public AccountOutOfMoneyException(string message) :base(message){}
    }

    public class SmsTechAuthenticationFailed : Exception
    {
        public SmsTechAuthenticationFailed(string message) : base(message){}
    }
}
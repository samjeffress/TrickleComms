using System;

namespace SmsActioner
{
    public class AccountOutOfMoneyException : Exception
    {
        public AccountOutOfMoneyException(string message) :base(message){}
    }
}
namespace ConfigurationModels
{
    public class SmsProviderConfiguration
    {
        public SmsProvider? SmsProvider { get; set; }
    }

    public enum SmsProvider
    {
        Twilio,
        Nexmo
    }
}
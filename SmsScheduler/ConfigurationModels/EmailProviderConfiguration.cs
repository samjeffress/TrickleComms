namespace ConfigurationModels
{
    public class EmailProviderConfiguration
    {
        public EmailProvider EmailProvider { get; set; }
    }

    public enum EmailProvider
    {
        NoEmailFunctionality,
        Mandrill,
        Mailgun
    }
}
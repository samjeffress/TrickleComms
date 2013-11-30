using NServiceBus;

namespace IncomingSmsHandler
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Worker, UsingTransport<AzureServiceBus>
    {
    }
}

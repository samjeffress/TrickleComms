using NServiceBus;

namespace SmsCoordinator
{
    public class EndpointConfig : IConfigureThisEndpoint, AsA_Publisher
    {
    }
}
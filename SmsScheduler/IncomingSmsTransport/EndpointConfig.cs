using NServiceBus;
using NServiceBus.Features;

namespace IncomingSmsTransport
{
	public class EndpointConfig : IConfigureThisEndpoint, AsA_Server, UsingTransport<AzureServiceBus>, IWantCustomInitialization
	{
	    public void Init()
	    {
            Configure.Serialization.Xml();
            Configure.With()
                .DefaultBuilder()
                    .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                    .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                    .DefineEndpointName("IncomingSmsHandler")
                .UnicastBus()
                    .LoadMessageHandlers();
            Configure.Features.Disable<Sagas>();
            Configure.Features.Disable<TimeoutManager>();
	    }
	}
}

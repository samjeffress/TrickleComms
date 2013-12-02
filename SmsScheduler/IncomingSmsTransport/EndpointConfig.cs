using NServiceBus;
using NServiceBus.Features;

namespace IncomingSmsTransport
{
	public class EndpointConfig : IWantCustomInitialization,IConfigureThisEndpoint, AsA_Server, UsingTransport<AzureServiceBus>
	{
	    public void Init()
	    {
            var configure = Configure.With()
                .DefaultBuilder()
                    .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                    .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                    .DefiningMessagesAs(t => t.Namespace != null && t.Namespace.EndsWith("Messages"))
                    .DefiningMessagesAs(t => t.Namespace == "SmsMessages")
                    .DefiningMessagesAs(t => t.Namespace == "SmsTrackingMessages.Messages")
                .Log4Net()
                .UnicastBus()
                    .LoadMessageHandlers();

	        Configure.Features.Disable<Sagas>();
	        Configure.Features.Disable<TimeoutManager>();

            configure.CreateBus().Start();
	    }
	}
}

using NServiceBus;
using NServiceBus.Features;

namespace SmsScheduler
{
    public class EndpointConfig : IConfigureThisEndpoint, UsingTransport<Msmq>, IWantCustomInitialization, AsA_Publisher
    {
        public void Init()
        {
            Configure.Features.Enable<Sagas>();
            Configure.Transactions.Enable();
            Configure.Serialization.Xml();
            var configure = Configure.With().DefaultBuilder();

            Configure.Instance.RavenPersistence();
            Configure.Instance.RavenSubscriptionStorage();
            Configure.Instance.RavenSagaPersister();

            configure
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                .DefiningMessagesAs(t => t.Namespace != null && 
                    t.Namespace.StartsWith("SmsMessages") && (t.Namespace.EndsWith("Messages") || t.Namespace.EndsWith("Responses")))
                //.DefiningMessagesAs(t => t.Namespace == "SmsMessages")
                //.DefiningMessagesAs(t => t.Namespace == "SmsTrackingMessages.Messages")
            .Log4Net()
                .PurgeOnStartup(false)
            .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers();
            
            Configure.Instance.Configurer.ConfigureComponent<RavenDocStore>(DependencyLifecycle.SingleInstance);

            //configure.CreateBus().Start();
            //.Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());
        }
    }
}
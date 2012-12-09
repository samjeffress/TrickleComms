using NServiceBus;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions; // Used for EnsureDatabaseExists

namespace SmsTracking
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
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
            .XmlSerializer()
            .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
            .Sagas()
                .RavenSagaPersister()
            .UnicastBus()
                .LoadMessageHandlers();

            Configure.Instance.Configurer.ConfigureComponent<RavenDocStore>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<EmailService>(DependencyLifecycle.InstancePerUnitOfWork);
     
            configure.CreateBus()
            .Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());
        }
    }

    public class RavenDocStore : IRavenDocStore
    {
        private readonly IDocumentStore _documentStore;
        public RavenDocStore()
        {
            _documentStore = new DocumentStore { Url = "http://localhost:8080", DefaultDatabase = "SmsTracking" };
            _documentStore.Initialize();
            //_documentStore.DatabaseCommands.EnsureDatabaseExists("TwilioConfiguration");
            _documentStore.DatabaseCommands.EnsureDatabaseExists("SmsTracking");
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }

    public interface IRavenDocStore
    {
        IDocumentStore GetStore();
    }
}

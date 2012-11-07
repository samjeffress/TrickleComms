using NServiceBus;
using Raven.Client;
using Raven.Client.Document;

namespace SmsTracking
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization
    {
        public void Init()
        {
            var configure = Configure.With()
            .DefaultBuilder()
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
            _documentStore = new DocumentStore { Url = "http://localhost:8080" };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("TwilioConfiguration");
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

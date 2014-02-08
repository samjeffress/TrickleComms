using NServiceBus;

namespace SmsActioner
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, AsA_Publisher
    {
        public void Init()
        {
            var configure = Configure.With()
            .DefaultBuilder()
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
            .RunTimeoutManager()
            .Log4Net()
            .XmlSerializer()
            .MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
            .RavenPersistence()
            .Sagas()
                .RavenSagaPersister()
            .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers();

            const string listeningOn = "http://*:8888/";
            var appHost = new AppHost();
            appHost.Init();
            appHost.Start(listeningOn);

            Configure.Instance.Configurer.ConfigureComponent<RavenDocStore>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<SmsService>(DependencyLifecycle.InstancePerUnitOfWork);
            Configure.Instance.Configurer.ConfigureComponent<SmsTechWrapper>(DependencyLifecycle.InstancePerUnitOfWork);

            var bus = configure.CreateBus().Start();            //.Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());

            appHost.Container.Register(bus);
            appHost.Container.RegisterAutoWiredAs<RavenDocStore, IRavenDocStore>();//.RegisterAs<IRavenDocStore>(new RavenDocStore());
        }
    }
}
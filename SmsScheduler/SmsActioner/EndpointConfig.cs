using Funq;
using NServiceBus;
using NServiceBus.Features;
using NServiceBus.Installation.Environments;

namespace SmsActioner
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, AsA_Publisher, UsingTransport<Msmq> // , IWantToRunWhenBusStartsAndStops
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
            .DefaultBuilder()
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                .DefiningMessagesAs(t => t.Namespace != null &&
                    (t.Namespace.StartsWith("SmsMessages") || t.Namespace.StartsWith("SmsActioner.InternalMessages"))
                    && (t.Namespace.EndsWith(".Messages") || t.Namespace.EndsWith(".Responses")))
            .Log4Net()
                .IsTransactional(true)
                .PurgeOnStartup(false)
            .RavenPersistence()
                .RavenSagaPersister()
            .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers();


            Configure.Instance.Configurer.ConfigureComponent<RavenDocStore>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<SmsService>(DependencyLifecycle.InstancePerUnitOfWork);
            Configure.Instance.Configurer.ConfigureComponent<TwilioWrapper>(DependencyLifecycle.InstancePerUnitOfWork);
            
        //var bus = configure.CreateBus()
        //.Start(() => Configure.Instance.ForInstallationOn<Windows>().Install());
            //var bus = configure.CreateBus().Start();            //.Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());


            //// using the bus in this context is too early - shouldn't actually be started here
            //const string listeningOn = "http://*:8888/";
            //var appHost = new AppHost();
            //appHost.Init();
            //appHost.Start(listeningOn);
            ////appHost.Container.Register(bus);
            //appHost.Container.RegisterAutoWired<IBus>();
            //appHost.Container.RegisterAutoWiredAs<RavenDocStore, IRavenDocStore>();//.RegisterAs<IRavenDocStore>(new RavenDocStore());
        }

        //public void Start()
        //{
        //    const string listeningOn = "http://*:8888/";
        //    var appHost = new AppHost();
        //    appHost.Init();
        //    appHost.Start(listeningOn);
        //    appHost.Container.RegisterAutoWired<IBus>();
        //    appHost.Container.RegisterAutoWiredAs<RavenDocStore, IRavenDocStore>();//.RegisterAs<IRavenDocStore>(new RavenDocStore());
        //}

        //public void Stop()
        //{
        //}
    }
}
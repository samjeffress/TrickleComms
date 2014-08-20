using NServiceBus;
using NServiceBus.Features;
using SmsCoordinator.Email;
using SmsMessages.Scheduling.Events;

namespace SmsCoordinator
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, AsA_Publisher, UsingTransport<Msmq>
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
            //.RunTimeoutManager()
            .Log4Net()
            //.MsmqTransport()
                .IsTransactional(true)
                .PurgeOnStartup(false)
            .RavenPersistence()
                .RavenSagaPersister()
            .UnicastBus()
                .ImpersonateSender(false)
                .LoadMessageHandlers();
            //                .RavenSubscriptionStorage();


            Configure.Instance.Configurer.ConfigureComponent<RavenDocStore>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<CalculateSmsTiming>(DependencyLifecycle.InstancePerUnitOfWork);
            Configure.Instance.Configurer.ConfigureComponent<RavenScheduleDocuments>(DependencyLifecycle.InstancePerUnitOfWork);
            Configure.Instance.Configurer.ConfigureComponent<MailActioner>(DependencyLifecycle.InstancePerCall);
            Configure.Instance.Configurer.ConfigureComponent<DateTimeUtcFromOlsenMapping>(DependencyLifecycle.SingleInstance);

            //configure.CreateBus().Start();
            //.Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());
        }

    }

    public class StartUp : IWantToRunWhenBusStartsAndStops
    {
        public IBus Bus { get; set; }

        public void Start()
        {
            // NOTE: To remove messages that were previously used, but no longer needed.
            Bus.Unsubscribe<SmsScheduled>();
            Bus.Unsubscribe<MessageRescheduled>();
            Bus.Unsubscribe<MessageSchedulePaused>();
            Bus.Unsubscribe<ScheduledSmsSent>();
            Bus.Unsubscribe<ScheduledSmsFailed>();
        }

        public void Stop()
        {
        }
    }
}
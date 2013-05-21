using NServiceBus;
using SmsMessages.Scheduling.Events;

namespace SmsCoordinator
{
    public class EndpointConfig : IConfigureThisEndpoint, IWantCustomInitialization, AsA_Publisher
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
//                .RavenSubscriptionStorage();

            Configure.Instance.Configurer.ConfigureComponent<RavenDocStore>(DependencyLifecycle.SingleInstance);
            Configure.Instance.Configurer.ConfigureComponent<CalculateSmsTiming>(DependencyLifecycle.InstancePerUnitOfWork);
            Configure.Instance.Configurer.ConfigureComponent<RavenScheduleDocuments>(DependencyLifecycle.InstancePerUnitOfWork);

            configure.CreateBus()
            .Start(() => Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());
        }

    }

    public class StartUp : IWantToRunAtStartup
    {
        public IBus Bus { get; set; }

        public void Run()
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
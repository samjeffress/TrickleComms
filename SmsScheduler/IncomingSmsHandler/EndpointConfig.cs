using NServiceBus;
using NServiceBus.Features;

namespace IncomingSmsHandler
{
    public static class EndpointConfig
    {
        public static IBus Bus { get; set; }

        public static void ConfigureNServiceBus()
        {
            Configure.With()
                     .DefaultBuilder()
                     .DefiningMessagesAs(
                         t =>
                         t.Namespace != null &&
                         (t.Namespace.EndsWith("Requests") || t.Namespace.EndsWith("Responses") ||
                          t.Namespace.EndsWith("Timeouts")))
                     .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                     .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                     .AzureConfigurationSource()
                     .UseTransport<AzureServiceBus>()
                     .MessageForwardingInCaseOfFault();
            Configure.Features.Disable<SecondLevelRetries>();
            Configure.Features.Disable<TimeoutManager>();
            Configure.Features.Disable<Sagas>();
            Configure.Serialization.Xml();

            Bus = Configure.With()
                           .PurgeOnStartup(false)
                           .UnicastBus()
                           .CreateBus().Start();
        }
    }

    //public class Endpoint : IConfigureThisEndpoint, AsA_Worker, IWantCustomInitialization
    //{
    //    public static IBus Bus { get; set; }

    //    public void Init()
    //    {
    //        Configure.With()
    //             .DefaultBuilder()
    //             .DefiningMessagesAs(
    //                 t =>
    //                 t.Namespace != null &&
    //                 (t.Namespace.EndsWith("Requests") || t.Namespace.EndsWith("Responses") ||
    //                  t.Namespace.EndsWith("Timeouts")))
    //             .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
    //             .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
    //             .AzureConfigurationSource()
    //             .UseTransport<AzureServiceBus>()
    //             .MessageForwardingInCaseOfFault();
    //                Configure.Features.Disable<SecondLevelRetries>();
    //                Configure.Features.Disable<TimeoutManager>();
    //                Configure.Features.Disable<Sagas>();
    //                Configure.Serialization.Xml();
    //        Configure.Transactions.Enable();

    //        Configure.Component(typeof (AppHost), DependencyLifecycle.InstancePerUnitOfWork);
    //                //Bus = Configure.With()
    //                //               .PurgeOnStartup(false)
    //                //               .UnicastBus()
    //                //               .CreateBus().Start();
    //    }
    //}
}

using System.Reflection;
using NServiceBus;
using NServiceBus.Features;

namespace SmsWeb.App_Start
{
    public static class NServiceBusConfig
    {
        private static IBus _bus;

        public static readonly Configure Config;

        static NServiceBusConfig()
        {
            Config = Configure.With(new Assembly[] 
                { 
                    typeof(SmsMessages.Coordinator.Commands.PauseTrickledMessagesIndefinitely).Assembly,
                    typeof(SmsWeb.SmsScheduleStatusHandler).Assembly,
                })
                .DefineEndpointName("SmsWeb")
                .DefaultBuilder()
                .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                .DefiningMessagesAs(t => t.Namespace != null && !t.Namespace.Contains("NServiceBus") &&
                    (t.Namespace.EndsWith(".Messages") || t.Namespace.EndsWith(".Responses")))
                    .DisableTimeoutManager()
                .MsmqTransport()
                .MessageForwardingInCaseOfFault()
                .PurgeOnStartup(false)
                .UnicastBus()
                .LoadMessageHandlers();
            Configure.Features.Disable<SecondLevelRetries>();
            
            Config.Configurer.ConfigureComponent<RavenDocStore>(DependencyLifecycle.SingleInstance);
        }

        public static IBus BusInstance()
        {
            if (_bus == null)
            {
                if (Configure.WithHasBeenCalled())
                {
                    _bus = Config.CreateBus().Start();
                    return _bus;
                }
                _bus = Config.CreateBus().Start();
            }
            return _bus;
        }
    }
}
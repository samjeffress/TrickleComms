using System.Web.Routing;
using NavigationRoutes;
using SmsWeb.Controllers;

namespace BootstrapMvcSample
{
    public class ExampleLayoutsRouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapNavigationRoute<SendNowController>("Send Now", c => c.Create());
            routes.MapNavigationRoute<ScheduleController>("Schedule", c => c.Create());
            routes.MapNavigationRoute<CoordinatorController> ("Coordinate", c  => c.Index())
                .AddChildRoute<CoordinatorController>("Create", c => c.Create())
                .AddChildRoute<CoordinatorController>("History", c => c.History(0, 20));
            routes.MapNavigationRoute<HomeController>("Configuration", c => c.Configuration())
                .AddChildRoute<TwilioConfigController>("Twilio", t => t.Details())
                .AddChildRoute<MailgunConfigController>("Mailgun", m => m.Details());
        }
    }
}

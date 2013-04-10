using System.Web.Routing;
using NavigationRoutes;
using SmsWeb.Controllers;

namespace BootstrapMvcSample
{
    public class ExampleLayoutsRouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.MapNavigationRoute<CoordinatorController>("Create", c => c.Create());
            routes.MapNavigationRoute<CoordinatorController>("History", c => c.History(0, 20));
            routes.MapNavigationRoute("API", "api", "api");
            routes.MapNavigationRoute<HomeController>("Configuration", c => c.Configuration())
                .AddChildRoute<TwilioConfigController>("Twilio", t => t.Details())
                .AddChildRoute<MailgunConfigController>("Mailgun", m => m.Details())
                .AddChildRoute<DefaultEmailController>("Email Notifications", e => e.Details())
                .AddChildRoute<CountryCodeConfigController>("Country Code Default", e => e.Index());
        }
    }
}

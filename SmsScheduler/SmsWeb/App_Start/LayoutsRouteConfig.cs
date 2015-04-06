using System.Web.Routing;
using NavigationRoutes;
using SmsWeb.API;
using SmsWeb.Controllers;

namespace SmsWeb.App_Start
{
    public class LayoutsRouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {

            routes.MapNavigationRoute<SmsAndEmailController>("Create", c => c.Create());
            routes.MapNavigationRoute<CoordinatorController>("History", c => c.History(0, 20));
//            routes.MapNavigationRoute<CoordinatorController>("Create", c => c.Create());
//            routes.MapNavigationRoute<CoordinatorController>("History", c => c.History(0, 20));
            routes.MapNavigationRoute("API", "api", "api");
            routes.MapNavigationRoute<CommunicationTemplateController>("Templates", c => c.Index());
            routes.MapNavigationRoute<HomeController>("Configuration", c => c.Configuration());
        }
    }
}

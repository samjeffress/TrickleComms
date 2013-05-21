using NServiceBus;
using System.Web.Mvc;
using Raven.Client;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.Mvc;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;
using SmsWeb.API;
using SmsWeb.Controllers;
using Schedule = SmsWeb.API.Schedule;

[assembly: WebActivator.PreApplicationStartMethod(typeof(SmsWeb.App_Start.AppHost), "Start")]

//IMPORTANT: Add the line below to MvcApplication.RegisterRoutes(RouteCollection) in the Global.asax:
//routes.IgnoreRoute("api/{*pathInfo}"); 

/**
 * Entire ServiceStack Starter Template configured with a 'Hello' Web Service and a 'Todo' Rest Service.
 *
 * Auto-Generated Metadata API page at: /metadata
 * See other complete web service examples at: https://github.com/ServiceStack/ServiceStack.Examples
 */

namespace SmsWeb.App_Start
{
	//A customizeable typed UserSession that can be extended with your own properties
	//To access ServiceStack's Session, Cache, etc from MVC Controllers inherit from ControllerBase<CustomUserSession>
	public class CustomUserSession : AuthUserSession
	{
		public string CustomProperty { get; set; }
	}

	public class AppHost : AppHostBase
	{		
		public AppHost() //Tell ServiceStack the name and where to find your web services
			: base("StarterTemplate ASP.NET Host", typeof(SmsService).Assembly) { }

		public override void Configure(Funq.Container container)
		{
			//Set JSON web services to return idiomatic JSON camelCase properties
			ServiceStack.Text.JsConfig.EmitCamelCaseNames = true;

			//Configure User Defined REST Paths
		    Routes
		        .Add<Sms>("/sms")
                .Add<Schedule>("/schedule")
                .Add<Coordinator>("/coordinator");

			//Change the default ServiceStack configuration
			//SetConfig(new EndpointHostConfig {
			//    DebugMode = true, //Show StackTraces in responses in development
			//});

			//Enable Authentication
			//ConfigureAuth(container);

			//Register all your dependencies
            //container.Register(new TodoRepository());
			
			//Register In-Memory Cache provider. 
			//For Distributed Cache Providers Use: PooledRedisClientManager, BasicRedisClientManager or see: https://github.com/ServiceStack/ServiceStack/wiki/Caching
			container.Register<ICacheClient>(new MemoryCacheClient());
			container.Register<ISessionFactory>(c => 
				new SessionFactory(c.Resolve<ICacheClient>()));

		    container.Register<IRavenDocStore>(new RavenDocStore());
            container.Register<IDateTimeUtcFromOlsenMapping>(new DateTimeUtcFromOlsenMapping());
            container.Register<ICoordinatorModelToMessageMapping>(new CoordinatorModelToMessageMapping(new DateTimeUtcFromOlsenMapping()));
		    container.Register<ICoordinatorApiModelToMessageMapping>(new CoordinatorApiModelToMessageMapping());
            
		    
            var busConfig = NServiceBus.Configure.With()
                .DefineEndpointName("SmsWeb")
                .DefaultBuilder()
                    .DefiningCommandsAs(t => t.Namespace != null && t.Namespace.EndsWith("Commands"))
                    .DefiningEventsAs(t => t.Namespace != null && t.Namespace.EndsWith("Events"))
                    .DefiningMessagesAs(t => t.Namespace == "SmsMessages")
                    .Log4Net()
                .XmlSerializer()
                .MsmqTransport()
                    .IsTransactional(true)
                    .PurgeOnStartup(false)
                .UnicastBus()
                    .LoadMessageHandlers();

            //busConfig.Configurer.ConfigureComponent<DateTimeUtcFromOlsenMapping>(DependencyLifecycle.SingleInstance);

		    var bus = busConfig.CreateBus().Start(() => NServiceBus.Configure.Instance.ForInstallationOn<NServiceBus.Installation.Environments.Windows>().Install());

		    container.Register(bus);
			//Set MVC to use the same Funq IOC as ServiceStack
			ControllerBuilder.Current.SetControllerFactory(new FunqControllerFactory(container));
		}

		/* Uncomment to enable ServiceStack Authentication and CustomUserSession
		private void ConfigureAuth(Funq.Container container)
		{
			var appSettings = new AppSettings();

			//Default route: /auth/{provider}
			Plugins.Add(new AuthFeature(this, () => new CustomUserSession(),
				new IAuthProvider[] {
					new CredentialsAuthProvider(appSettings), 
					new FacebookAuthProvider(appSettings), 
					new TwitterAuthProvider(appSettings), 
					new BasicAuthProvider(appSettings), 
				})); 

			//Default route: /register
			Plugins.Add(new RegistrationFeature()); 

			//Requires ConnectionString configured in Web.Config
			var connectionString = ConfigurationManager.ConnectionStrings["AppDb"].ConnectionString;
			container.Register<IDbConnectionFactory>(c =>
				new OrmLiteConnectionFactory(connectionString, SqlServerOrmLiteDialectProvider.Instance));

			container.Register<IUserAuthRepository>(c =>
				new OrmLiteAuthRepository(c.Resolve<IDbConnectionFactory>()));

			var authRepo = (OrmLiteAuthRepository)container.Resolve<IUserAuthRepository>();
			authRepo.CreateMissingTables();
		}
		*/

		public static void Start()
		{
			new AppHost().Init();
		}
	}
}
using System;
using System.Collections.Generic;
using NServiceBus;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;

namespace SmsActioner
{
    public class SmsReceivedHandler : Service
    {
        public IBus Bus { get; set; }

        public IRavenDocStore RavenDocStore { get; set; }

        [Authenticate]
        public void Get(SmsReceieved smsReceieved)
        {
            // publish message   
            using (var session = RavenDocStore.GetStore().OpenSession())
            {
                session.Store(smsReceieved);
                session.SaveChanges();
            }

            Console.WriteLine("Wassup!");
        }
    }

    public class SmsReceieved
    {
        public string Sid { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateUpdated { get; set; }
        public DateTime DateSent { get; set; }
        public string AccountSid { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string Body { get; set; }
        public string Status { get; set; }
        public string Direction { get; set; }
        public decimal Price { get; set; } 
        public string PriceUnit { get; set; }
        public string ApiVersion { get; set; }
    }

    public class AppHost : AppHostHttpListenerBase
    {
        public AppHost() : base("StarterTemplate HttpListener", typeof(SmsReceieved).Assembly) { }

        public override void Configure(Funq.Container container)
        {
            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] { new BasicAuthProvider() }));
            Plugins.Add(new RegistrationFeature());
            container.Register<ICacheClient>(new MemoryCacheClient());
            //var userRep = new InMemoryAuthRepository();
            var userRep = new BasicAuthImpl();
            container.Register<IUserAuthRepository>(userRep);
            Routes.Add<SmsReceieved>("/SmsIncoming/");
        }
    }

    public class BasicAuthImpl : IUserAuthRepository
    {
        public UserAuth CreateUserAuth(UserAuth newUser, string password)
        {
            throw new NotImplementedException();
        }

        public UserAuth UpdateUserAuth(UserAuth existingUser, UserAuth newUser, string password)
        {
            throw new NotImplementedException();
        }

        public UserAuth GetUserAuthByUserName(string userNameOrEmail)
        {
            throw new NotImplementedException();
        }

        public bool TryAuthenticate(string userName, string password, out UserAuth userAuth)
        {
            if (!string.IsNullOrWhiteSpace(userName) && userName.Equals("Aladdin")
                && !string.IsNullOrWhiteSpace(password) && password.Equals("open sesame"))
            {
                userAuth = new UserAuth();
                return true;
            }
            userAuth = null;
            return false;
        }

        public bool TryAuthenticate(Dictionary<string, string> digestHeaders, string PrivateKey, int NonceTimeOut, string sequence,
                                    out UserAuth userAuth)
        {
            throw new NotImplementedException();
        }

        public void LoadUserAuth(IAuthSession session, IOAuthTokens tokens)
        {
            throw new NotImplementedException();
        }

        public UserAuth GetUserAuth(string userAuthId)
        {
            throw new NotImplementedException();
        }

        public void SaveUserAuth(IAuthSession authSession)
        {
            throw new NotImplementedException();
        }

        public void SaveUserAuth(UserAuth userAuth)
        {
            throw new NotImplementedException();
        }

        public List<UserOAuthProvider> GetUserOAuthProviders(string userAuthId)
        {
            return new List<UserOAuthProvider>();
            //throw new NotImplementedException();
        }

        public UserAuth GetUserAuth(IAuthSession authSession, IOAuthTokens tokens)
        {
            throw new NotImplementedException();
        }

        public string CreateOrMergeAuthSession(IAuthSession authSession, IOAuthTokens tokens)
        {
            throw new NotImplementedException();
        }
    }
}
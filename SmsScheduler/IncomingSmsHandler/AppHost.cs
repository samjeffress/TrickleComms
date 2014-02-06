using System;
using System.Collections.Generic;
using Funq;
using ServiceStack.CacheAccess;
using ServiceStack.CacheAccess.Providers;
using ServiceStack.ServiceInterface;
using ServiceStack.ServiceInterface.Auth;
using ServiceStack.WebHost.Endpoints;
using SmsMessages.MessageSending.Commands;
using WebActivatorEx;

[assembly: PreApplicationStartMethod(typeof(IncomingSmsHandler.AppHost), "Start")]
namespace IncomingSmsHandler
{
    public class AppHost : AppHostBase
    {
        public AppHost() : base("Incoming Sms", typeof(ReceivedMessageService).Assembly)
        {
        }

        public static void Start()
        {
            new AppHost().Init();
        }

        public override void Configure(Container container)
        {
            Plugins.Add(new AuthFeature(() => new AuthUserSession(), new IAuthProvider[] { new BasicAuthProvider() }));
            Plugins.Add(new RegistrationFeature());
            container.Register<ICacheClient>(new MemoryCacheClient());
            var userRep = new BasicAuthImpl();
            container.Register<IUserAuthRepository>(userRep);
            Routes
                .Add<MessageReceived>("/SmsIncoming");

            EndpointConfig.ConfigureNServiceBus();
            Container.Register(b => EndpointConfig.Bus);
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

        // TODO : Make this not be l33t hardcoded auth
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

        List<UserOAuthProvider> IUserAuthRepository.GetUserOAuthProviders(string userAuthId)
        {
            return GetUserOAuthProviders(userAuthId);
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
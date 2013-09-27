using System;
using System.Configuration;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;

namespace SmsActioner
{
    public interface IRavenDocStore
    {
        IDocumentStore GetStore();
    }
 
    public class RavenDocStore : IRavenDocStore
    {
        private readonly IDocumentStore _documentStore;
        public RavenDocStore()
        {
            _documentStore = new DocumentStore
                {
                    Url = ConfigurationManager.AppSettings["RavenUrl"], 
                    ApiKey = ConfigurationManager.AppSettings["RavenApiKey"], 
                    ResourceManagerId = Guid.NewGuid()
                };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("Configuration");
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }
}
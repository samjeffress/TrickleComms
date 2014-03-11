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

        string DatabaseName();
    }
 
    public class RavenDocStore : IRavenDocStore
    {
        private readonly IDocumentStore _documentStore;
        public RavenDocStore()
        {
            var apiKey = ConfigurationManager.AppSettings["RavenApiKey"];
            apiKey = string.IsNullOrWhiteSpace(apiKey) ? null : apiKey;
            _documentStore = new DocumentStore
                {
                    Url = ConfigurationManager.AppSettings["RavenUrl"], 
                    ApiKey = apiKey,
                    ResourceManagerId = Guid.NewGuid(),
                    DefaultDatabase = "SmsTracking"
                };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("Configuration");
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }

        public string DatabaseName()
        {
            return "samjeffress-SmsTracking";
        }
    }
}
using System.Configuration;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Extensions;

namespace SmsWeb
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
            _documentStore = new DocumentStore { Url = ConfigurationManager.AppSettings["RAVENHQ_CONNECTION_STRING"], DefaultDatabase = "SmsTracking" };
            _documentStore.Initialize();
            _documentStore.DatabaseCommands.EnsureDatabaseExists("Configuration");
            _documentStore.DatabaseCommands.EnsureDatabaseExists("SmsTracking");
        }

        public IDocumentStore GetStore()
        {
            return _documentStore;
        }
    }
}
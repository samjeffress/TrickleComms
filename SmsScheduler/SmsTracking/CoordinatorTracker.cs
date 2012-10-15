using NServiceBus;
using Raven.Client;

namespace SmsTracking
{
    public class CoordinatorTracker : 
        IHandleMessages<CoordinatorOverTimeCreated>
    {
        public IDocumentStore DocumentStore { get; set; }

        public void Handle(CoordinatorOverTimeCreated message)
        {
            throw new System.NotImplementedException();
        }
    }

    public class CoordinatorOverTimeCreated
    {
    }
}
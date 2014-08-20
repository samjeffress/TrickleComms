using SmsMessages.Coordinator.Events;

namespace SmsMessages.Email.Commands
{
    public class CoordinatorCreatedEmail
    {
        public CoordinatorCreated CoordinatorCreated { get; set; }

        public CoordinatorCreatedEmail()
        {
            CoordinatorCreated = new CoordinatorCreated();
        }

        public CoordinatorCreatedEmail(CoordinatorCreated coordinatorCreated)
        {
            CoordinatorCreated = coordinatorCreated;
        }
    }
}
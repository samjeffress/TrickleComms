using System.Web;

namespace SmsWeb
{
    // from http://volaresystems.com/Blog/post/2010/08/19/Dont-mock-HttpContext 
    public class CurrentUser : ICurrentUser
    {
        public virtual string Name()
        {
            return HttpContext.Current.User.Identity.Name;
        }

        public bool IsLoggedIn()
        {
            return HttpContext.Current.User.Identity.IsAuthenticated;
        }

        public bool IsAdmin()
        {
            return HttpContext.Current.User.IsInRole("Admin");
        }
    }

    public interface ICurrentUser
    {
        string Name();
        bool IsLoggedIn();
        bool IsAdmin();
    }
}
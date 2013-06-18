namespace CustomOAuthProvider.Controllers
{
    using System.Data.Entity;
    using System.Linq;
    using System.Web.Mvc;

    using CustomOAuthProvider.Models;

    [Authorize]
    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            using (var db = new UsersContext())
            {
                return this.View(db.UserProfiles.Include(u => u.ExtraData).First(u => u.UserName == User.Identity.Name));
            }
        }
    }
}
namespace CustomOAuthProvider.Controllers
{
    using System.Web.Mvc;

    public class HomeController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            this.ViewBag.Message = "Modify this template to jump-start your ASP.NET MVC application.";

            return this.View();
        }
    }
}
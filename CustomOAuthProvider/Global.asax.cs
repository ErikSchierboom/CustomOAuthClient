namespace CustomOAuthProvider
{
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Routing;

    using CustomOAuthProvider.App_Start;

    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            AuthConfig.RegisterAuth();
        }
    }
}
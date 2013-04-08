namespace Otter
{
    using System.Web.Mvc;
    using System.Web.Routing;
    using Ninject;
    using Otter.Infrastructure;
    using Otter.Mvc.Infrastructure;

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            DependencyResolver.SetResolver(new NinjectDependencyResolver(new StandardKernel()));
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            AutomapperConfiguration.Configure();
        }
    }
}
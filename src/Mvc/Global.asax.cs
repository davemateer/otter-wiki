namespace Otter
{
    using System;
    using System.Configuration;
    using System.Web.Configuration;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Ninject;
    using Otter.Infrastructure;

    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            bool encrypt = bool.Parse(ConfigurationManager.AppSettings["otter:EncryptConnectionStrings"]);

            if (encrypt)
            {
                Configuration config = WebConfigurationManager.OpenWebConfiguration("~");
                ConfigurationSection connectionStrings = config.GetSection("connectionStrings");
                if (connectionStrings != null && !connectionStrings.SectionInformation.IsProtected)
                {
                    if (ConfigurationManager.ConnectionStrings["otter"].ConnectionString.Contains("$(ReplacableToken"))
                    {
                        throw new InvalidOperationException("Please set password for otter database");
                    }

                    connectionStrings.SectionInformation.ProtectSection("DataProtectionConfigurationProvider");
                    config.Save(ConfigurationSaveMode.Full);
                }
            }

            AreaRegistration.RegisterAllAreas();
            DependencyResolver.SetResolver(new NinjectDependencyResolver(new StandardKernel()));
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            AutomapperConfiguration.Configure();
        }
    }
}
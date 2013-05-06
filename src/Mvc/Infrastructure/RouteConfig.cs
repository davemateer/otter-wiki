﻿namespace Otter.Infrastructure
{
    using System.Web.Mvc;
    using System.Web.Routing;

    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                name: "ReadArticle",
                url: "kb/{id}/{revision}",
                defaults: new { controller = "Article", action = "Read", revision = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Article", action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
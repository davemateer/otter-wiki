//-----------------------------------------------------------------------
// <copyright file="Global.asax.cs" company="Dave Mateer">
// The MIT License (MIT)
//
// Copyright (c) 2014 Dave Mateer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Otter
{
    using System;
    using System.Configuration;
    using System.Web;
    using System.Web.Configuration;
    using System.Web.Mvc;
    using System.Web.Routing;
    using Ninject;
    using Otter.Infrastructure;

    public class MvcApplication : HttpApplication
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
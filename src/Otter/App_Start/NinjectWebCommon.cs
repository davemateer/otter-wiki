//-----------------------------------------------------------------------
// <copyright file="NinjectWebCommon.cs" company="Dave Mateer">
// The MIT License (MIT)
//
// Copyright (c) 2015 Dave Mateer
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
[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(Otter.NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(Otter.NinjectWebCommon), "Stop")]

namespace Otter
{
    using System;
    using System.Web;
    using Microsoft.Web.Infrastructure.DynamicModuleHelper;
    using Ninject;
    using Ninject.Web.Common;
    using Otter.Repository;
    using Otter.Repository.Concrete;

    public static class NinjectWebCommon
    {
        private static readonly Bootstrapper ApplicationBootstrapper = new Bootstrapper();

        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            ApplicationBootstrapper.Initialize(CreateKernel);
        }

        public static void Stop()
        {
            ApplicationBootstrapper.ShutDown();
        }

        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        private static void RegisterServices(IKernel kernel)
        {
            kernel.Bind<ApplicationDbContext>().ToSelf().InRequestScope();
            kernel.Bind<IArticleRepository>().To<ArticleRepository>().InRequestScope();
            kernel.Bind<ISecurityRepository>().To<SecurityRepository>().InRequestScope();
            kernel.Bind<ITextToHtmlConverter>().To<MarkdownConverter>().InRequestScope();
        }
    }
}
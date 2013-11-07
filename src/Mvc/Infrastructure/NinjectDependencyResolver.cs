﻿namespace Otter.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using Ninject;
    using Otter.Repository;
    using Ninject.Web.Common;
    using Otter.Repository.Abstract;
    using Otter.Repository.Concrete;

    public sealed class NinjectDependencyResolver : IDependencyResolver
    {
        private readonly IKernel ninjectKernel;

        public NinjectDependencyResolver(IKernel kernel)
        {
            this.ninjectKernel = kernel;
            this.AddBindings();
        }

        public object GetService(Type serviceType)
        {
            return this.ninjectKernel.TryGet(serviceType);
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return this.ninjectKernel.GetAll(serviceType);
        }

        private void AddBindings()
        {
            this.ninjectKernel.Bind<IApplicationDbContext>().To<ApplicationDbContext>().InRequestScope();
            this.ninjectKernel.Bind<IArticleRepository>().To<ArticleRepository>().InRequestScope();
            this.ninjectKernel.Bind<ISecurityRepository>().To<SecurityRepository>().InRequestScope();
            this.ninjectKernel.Bind<ITextToHtmlConverter>().To<MarkdownConverter>().InRequestScope();
        }
    }
}
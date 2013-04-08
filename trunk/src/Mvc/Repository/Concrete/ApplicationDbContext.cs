using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Otter.Mvc.Domain;
using Otter.Mvc.Repository.Abstract;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Otter.Mvc.Repository.Concrete
{
    public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext() : base("ThisApplication")
        {
        }

        public DbSet<Article> Articles
        {
            get { return this.Set<Article>(); }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
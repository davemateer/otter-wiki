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
        public ApplicationDbContext() : base("Otter")
        {
        }

        public DbSet<Article> Articles
        {
            get { return this.Set<Article>(); }
        }

        public DbSet<ArticleHistory> ArticleHistory
        {
            get { return this.Set<ArticleHistory>(); }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ArticleHistory>().HasKey(h => new { h.ArticleId, h.Revision });
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
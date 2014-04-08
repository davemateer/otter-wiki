using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using Otter.Domain;
using Otter.Repository.Abstract;
using System.Data.Entity.ModelConfiguration.Conventions;

namespace Otter.Repository.Concrete
{
    public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext() : base("Otter")
        {
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        public DbSet<Article> Articles
        {
            get { return this.Set<Article>(); }
        }

        public DbSet<ArticleHistory> ArticleHistory
        {
            get { return this.Set<ArticleHistory>(); }
        }

        public DbSet<ArticleSecurity> ArticleSecurity
        {
            get { return this.Set<ArticleSecurity>(); }
        }

        public DbSet<ArticleTag> ArticleTags
        {
            get { return this.Set<ArticleTag>(); }
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ArticleHistory>().HasKey(h => new { h.ArticleId, h.Revision });
            modelBuilder.Entity<ArticleTag>().HasKey(t => new { t.ArticleId, t.Tag });
            modelBuilder.Entity<ArticleSecurity>().HasKey(s => new { s.ArticleId, s.Scope, s.EntityId });
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
            base.OnModelCreating(modelBuilder);
        }
    }
}
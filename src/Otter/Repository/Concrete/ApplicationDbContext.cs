//-----------------------------------------------------------------------
// <copyright file="ApplicationDbContext.cs" company="Dave Mateer">
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
namespace Otter.Repository.Concrete
{
    using System.Data.Entity;
    using System.Data.Entity.ModelConfiguration.Conventions;
    using Otter.Domain;
    using Otter.Repository.Abstract;

    public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
    {
        public ApplicationDbContext()
            : base("Otter")
        {
            Database.SetInitializer<ApplicationDbContext>(null);
        }

        public DbSet<ArticleAttachment> ArticleAttachments
        {
            get { return this.Set<ArticleAttachment>(); }
        }

        public DbSet<ArticleHistory> ArticleHistory
        {
            get { return this.Set<ArticleHistory>(); }
        }

        public DbSet<ArticleImage> ArticleImages
        {
            get { return this.Set<ArticleImage>(); }
        }

        public DbSet<Article> Articles
        {
            get { return this.Set<Article>(); }
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
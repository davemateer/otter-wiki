using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Otter.Domain;
using System.Data.Entity;

namespace Otter.Repository.Abstract
{
    public interface IApplicationDbContext
    {
        DbSet<Article> Articles { get; }

        DbSet<ArticleHistory> ArticleHistory { get; }

        DbSet<ArticleTag> ArticleTags { get; }
    }
}
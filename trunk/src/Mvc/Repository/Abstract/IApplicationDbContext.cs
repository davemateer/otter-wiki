namespace Otter.Repository.Abstract
{
    using System.Data.Entity;
    using Otter.Domain;

    public interface IApplicationDbContext
    {
        DbSet<Article> Articles { get; }

        DbSet<ArticleHistory> ArticleHistory { get; }

        DbSet<ArticleSecurity> ArticleSecurity { get; }

        DbSet<ArticleTag> ArticleTags { get; }
    }
}
namespace Otter.Repository
{
    using System.Collections.Generic;
    using System.Linq;
    using Otter.Domain;
    using Otter.Models;

    public interface IArticleRepository
    {
        IQueryable<Article> Articles { get; }

        IQueryable<ArticleHistory> ArticleHistory { get; }

        IQueryable<ArticleSecurity> ArticleSecurity { get; }

        IQueryable<ArticleTag> ArticleTags { get; }

        string InsertArticle(string title, string text, IEnumerable<string> tags, IEnumerable<ArticleSecurity> security, string userId);

        void UpdateArticle(int articleId, string title, string urlTitle, string text, string comment, IEnumerable<string> tags, IEnumerable<ArticleSecurity> security, string userId);

        string GetRevisionHtml(int articleId, int revision);

        IEnumerable<ArticleSearchResult> Search(string query, string userId);

        void HydratePermissionModel(PermissionModel model, int articleId, string userId);
    }
}

namespace Otter.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using Otter.Domain;

    public interface IArticleRepository
    {
        IQueryable<Article> Articles { get; }

        IQueryable<ArticleHistory> ArticleHistory { get; }

        IQueryable<ArticleTag> ArticleTags { get; }

        string InsertArticle(string title, string text, IEnumerable<string> tags, string userId);

        void UpdateArticle(int articleId, string title, string urlTitle, string text, string comment, string userId);

        string GetRevisionHtml(int articleId, int revision);
    }
}

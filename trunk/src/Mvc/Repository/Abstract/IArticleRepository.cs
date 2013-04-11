namespace Otter.Mvc.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using Otter.Mvc.Domain;

    public interface IArticleRepository
    {
        IQueryable<Article> Articles { get; }

        IQueryable<ArticleHistory> ArticleHistory { get; }

        string InsertArticle(string title, string text, string userId);

        void UpdateArticle(int articleId, string title, string urlTitle, string text, string comment, string userId);
    }
}

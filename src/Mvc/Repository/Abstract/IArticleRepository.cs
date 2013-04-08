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

        string InsertArticle(string title, string text);
    }
}

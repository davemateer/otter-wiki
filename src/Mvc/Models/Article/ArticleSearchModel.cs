namespace Otter.Models
{
    using System.Collections.Generic;
    using Otter.Domain;

    public sealed class ArticleSearchModel
    {
        public string Query { get; set; }

        public IEnumerable<string> Tags { get; set; }

        public IEnumerable<ArticleSearchResult> Articles { get; set; }
    }
}
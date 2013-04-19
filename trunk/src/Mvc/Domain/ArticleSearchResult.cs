namespace Otter.Domain
{
    using System;

    public sealed class ArticleSearchResult
    {
        public string UrlTitle { get; set; }

        public string Title { get; set; }

        public string UpdatedBy { get; set; }

        public DateTime UpdatedDtm { get; set; }
    }
}
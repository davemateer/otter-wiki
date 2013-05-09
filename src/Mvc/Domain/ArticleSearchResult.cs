namespace Otter.Domain
{
    using System;
    using AutoMapper;

    public sealed class ArticleSearchResult
    {
        public string UrlTitle { get; set; }

        public string Title { get; set; }

        public string UpdatedBy { get; set; }

        [IgnoreMap]
        public string UpdatedByDisplayName { get; set; }

        public DateTime UpdatedDtm { get; set; }
    }
}
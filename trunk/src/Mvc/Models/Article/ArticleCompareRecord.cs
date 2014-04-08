namespace Otter.Models
{
    using System;
    using AutoMapper;

    public sealed class ArticleCompareRecord : IUpdatedArticle
    {
        public string Title { get; set; }

        public int Revision { get; set; }

        public DateTime UpdatedDtm { get; set; }

        public string UpdatedBy { get; set; }

        public string Comment { get; set; }

        [IgnoreMap]
        public string UpdatedByDisplayName { get; set; }
    }
}
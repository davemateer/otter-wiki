namespace Otter.Models
{
    using System;
    using System.Collections.Generic;
    using AutoMapper;

    public sealed class ArticleHistoryModel : IUpdatedArticle
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlTitle { get; set; }

        public int Revision { get; set; }

        public DateTime UpdatedDtm { get; set; }

        public string UpdatedBy { get; set; }

        public string Comment { get; set; }

        [IgnoreMap]
        public string UpdatedByDisplayName { get; set; }

        [IgnoreMap]
        public IEnumerable<ArticleHistoryRecord> HistoryRecords { get; set; }
    }
}
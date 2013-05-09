namespace Otter.Models
{
    using System;
    using AutoMapper;

    public sealed class ArticleReadModel
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlTitle { get; set; }

        public string Html { get; set; }

        public int Revision { get; set; }

        public DateTime UpdatedDtm { get; set; }

        public string UpdatedBy { get; set; }

        [IgnoreMap]
        public string UpdatedByDisplayName { get; set; }
    }
}
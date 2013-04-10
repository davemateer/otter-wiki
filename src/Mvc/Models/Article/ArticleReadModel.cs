﻿namespace Otter.Mvc.Models
{
    using System;

    public sealed class ArticleReadModel
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlTitle { get; set; }

        public string Html { get; set; }

        public int Revision { get; set; }

        public DateTime LastUpdatedDtm { get; set; }

        public string LastUpdatedBy { get; set; }
    }
}
namespace Otter.Mvc.Models
{
    using System;

    public sealed class ArticleEditModel
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlTitle { get; set; }

        public string Text { get; set; }

        public int Revision { get; set; }

        public DateTime LastUpdatedDtm { get; set; }

        public string LastUpdatedBy { get; set; }
    }
}
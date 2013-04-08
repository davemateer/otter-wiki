namespace Otter.Mvc.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public sealed class Article
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlFriendlyTitle { get; set; }

        public string Text { get; set; }

        public byte[] TextHash { get; set; }

        public int Revision { get; set; }

        public DateTime LastUpdatedDtm { get; set; }

        public string LastUpdatedBy { get; set; }
    }
}
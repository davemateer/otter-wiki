namespace Otter.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public abstract class ArticleBase
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlTitle { get; set; }

        public string Text { get; set; }

        public int Revision { get; set; }

        public DateTime UpdatedDtm { get; set; }

        public string UpdatedBy { get; set; }

        public string Comment { get; set; }
    }
}
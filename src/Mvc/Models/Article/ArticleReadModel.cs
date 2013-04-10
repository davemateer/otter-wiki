using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Otter.Mvc.Models
{
    public sealed class ArticleReadModel
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlFriendlyTitle { get; set; }

        public string Html { get; set; }

        public byte[] TextHash { get; set; }

        public int Revision { get; set; }

        public DateTime LastUpdatedDtm { get; set; }

        public string LastUpdatedBy { get; set; }
    }
}
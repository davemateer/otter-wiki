namespace Otter.Models
{
    using System;

    public sealed class ArticleHistoryRecord
    {
        public int Revision { get; set; }

        public DateTime UpdatedDtm { get; set; }

        public string UpdatedBy { get; set; }

        public string Comment { get; set; }
    }
}
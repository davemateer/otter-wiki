namespace Otter.Models
{
    using System.Collections.Generic;
    using DiffMatchPatch;

    public sealed class ArticleCompareModel
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlTitle { get; set; }

        public ArticleCompareRecord CompareFrom { get; set; }

        public ArticleCompareRecord CompareTo { get; set; }

        public string Diff { get; set; }
    }
}
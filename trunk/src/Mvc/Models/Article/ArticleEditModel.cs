namespace Otter.Models
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using AutoMapper;

    public sealed class ArticleEditModel
    {
        public int ArticleId { get; set; }

        public string Title { get; set; }

        public string UrlTitle { get; set; }

        public string Text { get; set; }

        public int Revision { get; set; }

        public DateTime UpdatedDtm { get; set; }

        public string UpdatedBy { get; set; }

        [IgnoreMap]
        [Display(Name="Reason for change")]
        public string Comment { get; set; }

        [IgnoreMap]
        public string Tags { get; set; }

        [IgnoreMap]
        public PermissionModel Security { get; set; }
    }
}
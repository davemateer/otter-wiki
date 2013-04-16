namespace Otter.Models
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using AutoMapper;

    public sealed class ArticleCreateModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Text { get; set; }

        [IgnoreMap]
        public string Tags { get; set; }
    }
}
namespace Otter.Mvc.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
using System.ComponentModel.DataAnnotations;

    public sealed class ArticleCreateModel
    {
        [Required]
        [StringLength(100)]
        public string Title { get; set; }

        [Required]
        public string Text { get; set; }
    }
}
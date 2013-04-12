namespace Otter.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public sealed class Article : ArticleBase
    {
        public string Html { get; set; }
    }
}
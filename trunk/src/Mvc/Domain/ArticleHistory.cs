namespace Otter.Mvc.Domain
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;

    public sealed class ArticleHistory : ArticleBase
    {
        public string Delta { get; set; }
    }
}
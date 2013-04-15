namespace Otter.Domain
{
    public sealed class Article : ArticleBase
    {
        public string Html { get; set; }

        public string UrlTitle { get; set; }
    }
}
namespace Otter.Domain
{
    public sealed class ArticleSecurity
    {
        public int ArticleId { get; set; }

        public string Scope { get; set; }

        public string EntityId { get; set; }

        public string Permission { get; set; }
    }
}
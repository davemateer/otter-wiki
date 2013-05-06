namespace Otter.Domain
{
    public sealed class ArticleSecurity
    {
        public const string PermissionView = "V";
        public const string PermissionModify = "M";
        public const string ScopeEveryone = "E";
        public const string ScopeGroup = "G";
        public const string ScopeIndividual = "I";

        public int ArticleId { get; set; }

        public string Scope { get; set; }

        public string EntityId { get; set; }

        public string Permission { get; set; }
    }
}
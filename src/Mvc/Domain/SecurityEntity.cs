namespace Otter.Domain
{
    public sealed class SecurityEntity
    {
        public string EntityId { get; set; }

        public string Name { get; set; }

        public bool IsGroup { get; set; }
    }
}
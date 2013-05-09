namespace Otter.Domain
{
    public sealed class SecurityEntity
    {
        public string EntityId { get; set; }

        public string Name { get; set; }

        public SecurityEntityTypes EntityType { get; set; }
    }
}
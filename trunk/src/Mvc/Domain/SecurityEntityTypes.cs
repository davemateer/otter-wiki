namespace Otter.Domain
{
    using System;

    [Flags]
    public enum SecurityEntityTypes
    {
        None = 0,
        User = 1,
        Group = 2,

        Any = User | Group
    }
}
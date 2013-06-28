namespace Otter.Repository
{
    using System.Collections.Generic;
    using Otter.Domain;

    public interface ISecurityRepository
    {
        IEnumerable<SecurityEntity> Search(string query);

        SecurityEntity Find(string value, SecurityEntityTypes option);

        string StandardizeUserId(string userId);
    }
}
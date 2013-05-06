namespace Otter.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Otter.Domain;

    public interface ISecurityRepository
    {
        IEnumerable<SecurityEntity> Search(string query);

        SecurityEntity Find(string value);

        string StandardizeUserId(string userId);
    }
}
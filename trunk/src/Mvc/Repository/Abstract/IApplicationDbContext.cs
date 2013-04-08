using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Otter.Mvc.Domain;
using System.Data.Entity;

namespace Otter.Mvc.Repository.Abstract
{
    public interface IApplicationDbContext
    {
        DbSet<Article> Articles { get; }
    }
}
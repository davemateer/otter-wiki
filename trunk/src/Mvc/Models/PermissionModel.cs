namespace Otter.Models
{
    using System.Collections.Generic;

    public sealed class PermissionModel
    {
        public PermissionOption ViewOption { get; set; }

        public string ViewAccounts { get; set; }

        public PermissionOption ModifyOption { get; set; }

        public string ModifyAccounts { get; set; }
    }
}
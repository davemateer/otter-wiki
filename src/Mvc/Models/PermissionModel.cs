namespace Otter.Models
{
    using System.Collections.Generic;

    public sealed class PermissionModel
    {
        public PermissionModel()
        {
            ReadAccounts = "test";
            EditAccounts = "edit test; test";
        }

        public PermissionOption ReadOption { get; set; }

        public string ReadAccounts { get; set; }

        public PermissionOption EditOption { get; set; }

        public string EditAccounts { get; set; }
    }
}
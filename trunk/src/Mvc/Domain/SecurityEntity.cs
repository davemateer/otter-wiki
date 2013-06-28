namespace Otter.Domain
{
    using System;
    using System.Globalization;
    using System.Text;

    public sealed class SecurityEntity
    {
        public string EntityId { get; set; }

        public string Name { get; set; }

        public SecurityEntityTypes EntityType { get; set; }

        public override string ToString()
        {
            var text = new StringBuilder(this.Name);

            if (!StringComparer.OrdinalIgnoreCase.Equals(this.EntityId, this.Name))
            {
                text.AppendFormat(CultureInfo.InvariantCulture, " [{0}]", this.EntityId);
            }

            if (this.EntityType == SecurityEntityTypes.Group)
            {
                text.Append(" (Group)");
            }

            return text.ToString();
        }

        public static bool TryParse(string s, out SecurityEntity result)
        {
            if (string.IsNullOrEmpty(s))
            {
                result = null;
            }
            else
            {

            }
        }
    }
}
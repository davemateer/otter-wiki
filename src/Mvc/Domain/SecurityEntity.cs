namespace Otter.Domain
{
    using System;
    using System.DirectoryServices;
    using System.Globalization;
    using System.Text;
    using System.Text.RegularExpressions;

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
                return false;
            }

            result = new SecurityEntity();

            if (s.EndsWith("(Group)", StringComparison.OrdinalIgnoreCase))
            {
                result.EntityType = SecurityEntityTypes.Group;
                s = s.Substring(0, s.Length - "(Group)".Length).TrimEnd(null);
            }
            else
            {
                result.EntityType = SecurityEntityTypes.User;
            }

            Regex idExpression = new Regex(@"\[(?<id>[^\]]+)\]$", RegexOptions.ExplicitCapture);
            var match = idExpression.Match(s);
            if (match.Success)
            {
                result.EntityId = match.Groups["id"].Value;
                result.Name = s.Substring(0, match.Index).TrimEnd(null);
            }
            else
            {
                result.EntityId = s.TrimEnd(null);
                result.Name = result.EntityId;
            }

            return true;
        }

        public static SecurityEntity FromSearchResult(SearchResult result, SecurityEntityTypes type)
        {
            var entity = new SecurityEntity();
            entity.EntityId = result.Properties["sAMAccountName"][0].ToString();
            entity.EntityType = type;
            entity.Name = result.Properties["displayName"].Count > 0 ? result.Properties["displayName"][0].ToString() : entity.EntityId;
            return entity;
        }
    }
}
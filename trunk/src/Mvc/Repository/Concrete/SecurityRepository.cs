namespace Otter.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Otter.Domain;
    using System.Text.RegularExpressions;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;

    public sealed class SecurityRepository : ISecurityRepository
    {
        public IEnumerable<SecurityEntity> Search(string query)
        {
            // Safe, but restrictive white-list method of preventing LDAP injection. If other characters are ever needed (accents, etc.),
            // this may need extended. This removes leading and trailing spaces and anything other than the 26 letters of the English alphabet, 
            // 0-9 numbers, and embedded spaces.
            query = Regex.Replace(query.Trim(), "[^A-Za-z0-9 ]", string.Empty);

            var matches = new List<SecurityEntity>();

            using (var searcher = new DirectorySearcher())
            {
                searcher.Filter = string.Format("(&(objectCategory=person)(objectClass=user)(|(givenName={0}*)(sn={0}*)(displayName={0}*)(sAMAccountName={0}*)))", query);

                using (SearchResultCollection results = searcher.FindAll())
                {
                    foreach (SearchResult found in results)
                    {
                        matches.Add(new SecurityEntity()
                        {
                            EntityId = found.Properties["sAMAccountName"][0].ToString(),
                            IsGroup = false,
                            Name = found.Properties["displayName"][0].ToString()
                        });
                    }
                }

                searcher.Filter = string.Format("(&(objectCategory=group)(cn={0}*))", query);

                using (SearchResultCollection results = searcher.FindAll())
                {
                    foreach (SearchResult found in results)
                    {
                        matches.Add(new SecurityEntity()
                        {
                            EntityId = found.Properties["cn"][0].ToString(),
                            IsGroup = true,
                            Name = found.Properties["cn"][0].ToString()
                        });
                    }
                }
            }

            return matches;
        }

        public SecurityEntity Find(string value)
        {
            var groupExpression = new Regex(@"^(?<id>.+) \(Group\)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
            var individualExpression = new Regex(@"\[(?<id>[\d\w]+)\]$", RegexOptions.ExplicitCapture);

            var match = groupExpression.Match(value);
            if (match.Success)
            {
                using (var searcher = new DirectorySearcher())
                {
                    searcher.Filter = string.Format("(&(objectCategory=group)(cn={0}))", match.Groups["id"].Value);
                    SearchResult group = searcher.FindOne();

                    if (group == null)
                    {
                        return null;
                    }

                    return new SecurityEntity()
                    {
                        EntityId = match.Groups["id"].Value,
                        IsGroup = true,
                        Name = match.Groups["id"].Value
                    };
                }
            }

            match = individualExpression.Match(value);
            if (match.Success)
            {
                using (var searcher = new DirectorySearcher())
                {
                    searcher.Filter = string.Format("(&(objectCategory=person)(objectClass=user)(sAMAccountName={0}))", value);
                    SearchResult user = searcher.FindOne();

                    if (user == null)
                    {
                        return null;
                    }

                    return new SecurityEntity()
                    {
                        EntityId = match.Groups["id"].Value,
                        IsGroup = false,
                        Name = user.Properties["displayName"][0].ToString()
                    };
                }
            }

            // No success the easy way; try the more flexible way.
            using (var context = new PrincipalContext(ContextType.Domain))
            {
                var identity = Principal.FindByIdentity(context, value);
                if (identity != null)
                {
                    return new SecurityEntity()
                    {
                        EntityId = identity.SamAccountName,
                        IsGroup = false,
                        Name = identity.DisplayName
                    };
                }

                var group = GroupPrincipal.FindByIdentity(context, value);
                if (group != null)
                {
                    return new SecurityEntity()
                    {
                        EntityId = group.SamAccountName,
                        IsGroup = true,
                        Name = group.Name
                    };
                }

                return null;
            }
        }

        public string StandardizeUserId(string userId)
        {
            if (userId != null && userId.IndexOf('\\') > 0 && !userId.EndsWith("\\"))
            {
                return userId.Substring(userId.LastIndexOf('\\') + 1);
            }

            return userId;
        }
    }
}
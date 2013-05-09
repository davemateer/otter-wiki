namespace Otter.Repository
{
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Text.RegularExpressions;
    using Otter.Domain;
    using System.Collections.Concurrent;

    public sealed class SecurityRepository : ISecurityRepository
    {
        private readonly static ConcurrentDictionary<string, SecurityEntity> cache = new ConcurrentDictionary<string, SecurityEntity>();

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
                            EntityType = SecurityEntityTypes.User,
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
                            EntityType = SecurityEntityTypes.Group,
                            Name = found.Properties["cn"][0].ToString()
                        });
                    }
                }
            }

            return matches;
        }

        public SecurityEntity Find(string value, SecurityEntityTypes option)
        {
            SecurityEntity cachedEntity;
            if (cache.TryGetValue(value, out cachedEntity) && option.HasFlag(cachedEntity.EntityType))
            {
                return cachedEntity;
            }

            if (option.HasFlag(SecurityEntityTypes.Group))
            {
                string query = null;
                if (option == SecurityEntityTypes.Group)
                {
                    query = value;
                }
                else
                {
                    var groupExpression = new Regex(@"^(?<id>.+) \(Group\)$", RegexOptions.ExplicitCapture | RegexOptions.IgnoreCase);
                    var match = groupExpression.Match(value);
                    if (match.Success)
                    {
                        query = match.Groups["id"].Value;
                    }
                }

                if (!string.IsNullOrEmpty(query))
                {
                    using (var searcher = new DirectorySearcher())
                    {
                        searcher.Filter = string.Format("(&(objectCategory=group)(cn={0}))", query);
                        SearchResult group = searcher.FindOne();

                        if (group == null)
                        {
                            return null;
                        }

                        cachedEntity = new SecurityEntity()
                        {
                            EntityId = query,
                            EntityType = SecurityEntityTypes.Group,
                            Name = query
                        };
                        cache.TryAdd(value, cachedEntity);
                        return cachedEntity;
                    }
                }
            }

            if (option.HasFlag(SecurityEntityTypes.User))
            {
                string query = null;
                if (option == SecurityEntityTypes.User)
                {
                    query = value;
                }
                else
                {
                    var individualExpression = new Regex(@"\[(?<id>[\d\w]+)\]$", RegexOptions.ExplicitCapture);
                    var match = individualExpression.Match(value);
                    if (match.Success)
                    {
                        query = match.Groups["id"].Value;
                    }
                }
                
                if (!string.IsNullOrEmpty(query))
                {
                    using (var searcher = new DirectorySearcher())
                    {
                        searcher.Filter = string.Format("(&(objectCategory=person)(objectClass=user)(sAMAccountName={0}))", query);
                        SearchResult user = searcher.FindOne();

                        if (user == null)
                        {
                            return null;
                        }

                        cachedEntity = new SecurityEntity()
                        {
                            EntityId = query,
                            EntityType = SecurityEntityTypes.User,
                            Name = user.Properties["displayName"][0].ToString()
                        };

                        cache.TryAdd(value, cachedEntity);
                        return cachedEntity;
                    }
                }
            }

            // No success the easy way; try the more flexible way.
            using (var context = new PrincipalContext(ContextType.Domain))
            {
                if (option.HasFlag(SecurityEntityTypes.User))
                {
                    var identity = Principal.FindByIdentity(context, value);
                    if (identity != null)
                    {
                        cachedEntity = new SecurityEntity()
                        {
                            EntityId = identity.SamAccountName,
                            EntityType = SecurityEntityTypes.User,
                            Name = identity.DisplayName
                        };

                        cache.TryAdd(value, cachedEntity);
                        return cachedEntity;
                    }
                }

                if (option.HasFlag(SecurityEntityTypes.Group))
                {
                    var group = GroupPrincipal.FindByIdentity(context, value);
                    if (group != null)
                    {
                        cachedEntity = new SecurityEntity()
                        {
                            EntityId = group.SamAccountName,
                            EntityType = SecurityEntityTypes.Group,
                            Name = group.Name
                        };

                        cache.TryAdd(value, cachedEntity);
                        return cachedEntity;
                    }
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
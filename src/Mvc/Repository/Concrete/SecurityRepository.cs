namespace Otter.Repository
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Text.RegularExpressions;
    using Otter.Domain;

    public sealed class SecurityRepository : ISecurityRepository
    {
        // Using ConcurrentDictionary because a new instance is created per request, but we are sharing this dictionary across instances.
        private readonly static ConcurrentDictionary<string, SecurityEntity> ldapCache = new ConcurrentDictionary<string, SecurityEntity>();

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
                        matches.Add(SecurityEntity.FromSearchResult(found, SecurityEntityTypes.User));
                    }
                }

                searcher.Filter = string.Format("(&(objectCategory=group)(|(cn={0}*)(displayName={0}*)(sAMAccountName={0}*)))", query);

                using (SearchResultCollection results = searcher.FindAll())
                {
                    foreach (SearchResult found in results)
                    {
                        matches.Add(SecurityEntity.FromSearchResult(found, SecurityEntityTypes.Group));
                    }
                }
            }

            return matches;
        }

        public SecurityEntity Find(string value, SecurityEntityTypes option)
        {
            // Is there already a value in the cache?
            SecurityEntity cachedEntity;
            if (ldapCache.TryGetValue(value, out cachedEntity) && option.HasFlag(cachedEntity.EntityType))
            {
                // Return the cached value.
                return cachedEntity;
            }

            // Nothing was matched in the cache. Search for a matching group.
            if (option.HasFlag(SecurityEntityTypes.Group))
            {
                string query = null;

                // If we are searching only for a group, the exact group id is expected to be the parameter value.
                if (option == SecurityEntityTypes.Group)
                {
                    query = value;
                }
                else
                {
                    SecurityEntity entity;
                    if (SecurityEntity.TryParse(value, out entity) && entity.EntityType == SecurityEntityTypes.Group)
                    {
                        query = entity.EntityId;
                    }
                }

                if (!string.IsNullOrEmpty(query))
                {
                    using (var searcher = new DirectorySearcher())
                    {
                        searcher.Filter = string.Format("(&(objectCategory=group)(sAMAccountName={0}))", query);
                        SearchResult group = searcher.FindOne();

                        if (group == null)
                        {
                            return null;
                        }

                        cachedEntity = SecurityEntity.FromSearchResult(group, SecurityEntityTypes.Group);
                        ldapCache.TryAdd(value, cachedEntity);
                        return cachedEntity;
                    }
                }
            }

            if (option.HasFlag(SecurityEntityTypes.User))
            {
                string query = null;

                // If we are searching only for a user, the exact user id is expected to be the parameter value.
                if (option == SecurityEntityTypes.User)
                {
                    query = value;
                }
                else
                {
                    SecurityEntity entity;
                    if (SecurityEntity.TryParse(value, out entity) && entity.EntityType == SecurityEntityTypes.User)
                    {
                        query = entity.EntityId;
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

                        cachedEntity = SecurityEntity.FromSearchResult(user, SecurityEntityTypes.User);
                        ldapCache.TryAdd(value, cachedEntity);
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

                        ldapCache.TryAdd(value, cachedEntity);
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
                            Name = group.DisplayName
                        };

                        ldapCache.TryAdd(value, cachedEntity);
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
//-----------------------------------------------------------------------
// <copyright file="SecurityRepository.cs" company="Dave Mateer">
// The MIT License (MIT)
//
// Copyright (c) 2014 Dave Mateer
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// </copyright>
//-----------------------------------------------------------------------
namespace Otter.Repository
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Security.Principal;
    using System.Text.RegularExpressions;
    using Otter.Domain;

    public sealed class SecurityRepository : ISecurityRepository
    {
        // Using ConcurrentDictionary because a new instance is created per request, but we are
        // sharing this dictionary across instances.
        private static readonly ConcurrentDictionary<string, SecurityEntity> LdapCache = new ConcurrentDictionary<string, SecurityEntity>();

        private static readonly string SecurityDomain = ConfigurationManager.AppSettings["otter:SecurityDomain"];

        public SecurityEntity Find(string value, SecurityEntityTypes option)
        {
            // Is there already a value in the cache?
            SecurityEntity cachedEntity;
            if (LdapCache.TryGetValue(value, out cachedEntity) && option.HasFlag(cachedEntity.EntityType))
            {
                // Return the cached value.
                return cachedEntity;
            }

            // Nothing was matched in the cache. Search for a matching group.
            if (option.HasFlag(SecurityEntityTypes.Group))
            {
                string query = null;

                // If we are searching only for a group, the exact group id is expected to be the
                // parameter value.
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
                        LdapCache.TryAdd(value, cachedEntity);
                        return cachedEntity;
                    }
                }
            }

            if (option.HasFlag(SecurityEntityTypes.User))
            {
                string query = null;

                // If we are searching only for a user, the exact user id is expected to be the
                // parameter value.
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
                        LdapCache.TryAdd(value, cachedEntity);
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

                        LdapCache.TryAdd(value, cachedEntity);
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

                        LdapCache.TryAdd(value, cachedEntity);
                        return cachedEntity;
                    }
                }

                return null;
            }
        }

        public IEnumerable<string> GetSecurityGroups(IIdentity identity)
        {
            List<string> groups = new List<string>();
            var windowsIdentity = identity as WindowsIdentity;

            if (windowsIdentity == null)
            {
                return null;
            }

            foreach (var group in windowsIdentity.Groups.Translate(typeof(NTAccount)))
            {
                if (group.Value.StartsWith(string.Format("{0}\\", SecurityDomain)))
                {
                    groups.Add(this.StandardizeUserId(group.Value));
                }
            }

            return groups;
        }

        public IEnumerable<SecurityEntity> Search(string query)
        {
            // Safe, but restrictive white-list method of preventing LDAP injection. If other
            // characters are ever needed (accents, etc.), this may need extended. This removes
            // leading and trailing spaces and anything other than the 26 letters of the English
            // alphabet, 0-9 numbers, and embedded spaces.
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
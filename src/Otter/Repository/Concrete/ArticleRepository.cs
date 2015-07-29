//-----------------------------------------------------------------------
// <copyright file="ArticleRepository.cs" company="Dave Mateer">
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
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.Entity;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Principal;
    using System.Transactions;
    using DiffMatchPatch;
    using Otter.Domain;
    using Otter.Models;
    using Otter.Repository.Concrete;

    public sealed class ArticleRepository : IArticleRepository
    {
        private static readonly ConcurrentDictionary<int, IEnumerable<ArticleSecurity>> ArticleSecurityCache = new ConcurrentDictionary<int, IEnumerable<ArticleSecurity>>();
        private static readonly string SecurityDomain = ConfigurationManager.AppSettings["otter:SecurityDomain"];

        private readonly ApplicationDbContext context;
        private readonly ITextToHtmlConverter converter;
        private readonly ISecurityRepository securityRepository;

        public ArticleRepository(ApplicationDbContext context, ITextToHtmlConverter converter, ISecurityRepository securityRepository)
        {
            this.context = context;
            this.converter = converter;
            this.securityRepository = securityRepository;
        }

        public IQueryable<ArticleAttachment> ArticleAttachments
        {
            get { return this.context.ArticleAttachments; }
        }

        public IQueryable<ArticleHistory> ArticleHistory
        {
            get { return this.context.ArticleHistory; }
        }

        public IQueryable<ArticleImage> ArticleImages
        {
            get { return this.context.ArticleImages; }
        }

        public IQueryable<Article> Articles
        {
            get { return this.context.Articles; }
        }

        public IQueryable<ArticleSecurity> ArticleSecurity
        {
            get { return this.context.ArticleSecurity; }
        }

        public IQueryable<ArticleTag> ArticleTags
        {
            get { return this.context.ArticleTags; }
        }

        public bool CanModify(IPrincipal user, int articleId)
        {
            string userId = this.securityRepository.StandardizeUserId(user.Identity.Name);

            IEnumerable<ArticleSecurity> security = this.GetSecurityRecords(articleId);

            foreach (var record in security)
            {
                if (record.Permission == Domain.ArticleSecurity.PermissionModify &&
                    (record.Scope == Domain.ArticleSecurity.ScopeEveryone ||
                     (record.Scope == Domain.ArticleSecurity.ScopeIndividual && StringComparer.OrdinalIgnoreCase.Equals(userId, record.EntityId)) ||
                     (record.Scope == Domain.ArticleSecurity.ScopeGroup && user.IsInRole(record.EntityId))))
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanView(IPrincipal user, int articleId)
        {
            string userId = this.securityRepository.StandardizeUserId(user.Identity.Name);

            IEnumerable<ArticleSecurity> security = this.GetSecurityRecords(articleId);

            foreach (var record in security)
            {
                if (record.Scope == Domain.ArticleSecurity.ScopeEveryone ||
                    (record.Scope == Domain.ArticleSecurity.ScopeIndividual && StringComparer.OrdinalIgnoreCase.Equals(userId, record.EntityId)) ||
                    (record.Scope == Domain.ArticleSecurity.ScopeGroup && user.IsInRole(record.EntityId)))
                {
                    return true;
                }
            }

            return false;
        }

        public void DeleteArticleAttachment(int articleAttachmentId)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.up_ArticleAttachment_Delete";
                cmd.Parameters.AddWithValue("@ArticleAttachmentId", articleAttachmentId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteArticleImage(int articleImageId)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.up_ArticleImage_Delete";
                cmd.Parameters.AddWithValue("@ArticleImageId", articleImageId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public string GetRevisionHtml(int articleId, int revision)
        {
            Article article = this.Articles.Single(a => a.ArticleId == articleId);
            string text = this.GetRevisionText(articleId, revision);
            return this.converter.Convert(text, article.UrlTitle);
        }

        public string GetRevisionText(int articleId, int revision)
        {
            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "up_Article_SelectRevisionTextDeltaSequence";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter[] parameters = new SqlParameter[2];

            parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
            parameters[0].Value = articleId;

            parameters[1] = new SqlParameter("@Revision", SqlDbType.Int);
            parameters[1].Value = revision;

            cmd.Parameters.AddRange(parameters);

            string text = string.Empty;

            conn.Open();

            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    int revisionOrdinal = reader.GetOrdinal("Revision");
                    int textOrdinal = reader.GetOrdinal("Text");

                    if (reader.Read())
                    {
                        // The first record is the base text.
                        text = reader.GetString(textOrdinal);

                        // Subsequent records are deltas to apply to the base text.
                        var patchUtil = new diff_match_patch();
                        while (reader.Read())
                        {
                            List<Patch> patches = patchUtil.patch_fromText(reader.GetString(textOrdinal));
                            object[] results = patchUtil.patch_apply(patches, text);
                            text = (string)results[0];
                        }
                    }
                }
            }
            finally
            {
                conn.Close();
            }

            return text;
        }

        public IEnumerable<Tuple<string, int>> GetTagSummary(IIdentity identity)
        {
            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "up_Article_GetSummaryByTag";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter[] parameters = new SqlParameter[2];

            parameters[0] = new SqlParameter("@UserId", SqlDbType.NVarChar, 256);
            parameters[0].Value = this.securityRepository.StandardizeUserId(identity.Name);

            parameters[1] = new SqlParameter("@UserGroups", SqlDbType.Structured);
            parameters[1].TypeName = "dbo.SecurityEntityTable";
            parameters[1].Value = this.GetGroupParameter(identity);

            cmd.Parameters.AddRange(parameters);

            conn.Open();

            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    var tags = new List<Tuple<string, int>>();

                    int tagOrdinal = reader.GetOrdinal("Tag");
                    int countOrdinal = reader.GetOrdinal("CountOfArticles");

                    while (reader.Read())
                    {
                        tags.Add(Tuple.Create(reader.IsDBNull(tagOrdinal) ? string.Empty : reader.GetString(tagOrdinal), reader.GetInt32(countOrdinal)));
                    }

                    return tags;
                }
            }
            finally
            {
                conn.Close();
            }
        }

        public void HydratePermissionModel(PermissionModel model, int articleId, string userId)
        {
            // Assume that permissions are specified groups and individuals. If we find an
            // "Everyone" scope in the security tokens, we will modify this.
            model.ModifyOption = PermissionOption.Specified;
            model.ViewOption = PermissionOption.Specified;

            var viewGroups = new List<string>();
            var viewUsers = new List<string>();
            var modifyGroups = new List<string>();
            var modifyUsers = new List<string>();

            var tokens = this.ArticleSecurity.Where(s => s.ArticleId == articleId);
            foreach (var token in tokens)
            {
                // If this is a view token and we've already learned that everyone can view, there's
                // no need to process the record.
                if (model.ViewOption == PermissionOption.Everyone && token.Permission == Otter.Domain.ArticleSecurity.PermissionView)
                {
                    continue;
                }

                // If this is a modify token and we've already learned that everyone can modify,
                // there's no need to process the record.
                if (model.ModifyOption == PermissionOption.Everyone && token.Permission == Otter.Domain.ArticleSecurity.PermissionModify)
                {
                    continue;
                }

                if (token.Scope == Otter.Domain.ArticleSecurity.ScopeEveryone)
                {
                    model.ViewOption = PermissionOption.Everyone;
                    viewGroups.Clear();
                    viewUsers.Clear();

                    if (token.Permission == Otter.Domain.ArticleSecurity.PermissionModify)
                    {
                        model.ModifyOption = PermissionOption.Everyone;
                        modifyGroups.Clear();
                        modifyUsers.Clear();
                    }
                }
                else if (token.Scope == Otter.Domain.ArticleSecurity.ScopeGroup)
                {
                    viewGroups.Add(token.EntityId);

                    if (token.Permission == Otter.Domain.ArticleSecurity.PermissionModify)
                    {
                        modifyGroups.Add(token.EntityId);
                    }
                }
                else if (token.Scope == Otter.Domain.ArticleSecurity.ScopeIndividual)
                {
                    viewUsers.Add(token.EntityId);

                    if (token.Permission == Otter.Domain.ArticleSecurity.PermissionModify)
                    {
                        modifyUsers.Add(token.EntityId);
                    }
                }
            }

            if (viewUsers.Count == 1 && viewUsers[0] == userId)
            {
                model.ViewOption = PermissionOption.JustMe;
                viewUsers.Clear();
            }

            if (modifyUsers.Count == 1 && modifyUsers[0] == userId)
            {
                model.ModifyOption = PermissionOption.JustMe;
                modifyUsers.Clear();
            }

            this.StandardizeAccounts(viewUsers, SecurityEntityTypes.User);
            this.StandardizeAccounts(viewGroups, SecurityEntityTypes.Group);
            this.StandardizeAccounts(modifyUsers, SecurityEntityTypes.User);
            this.StandardizeAccounts(modifyGroups, SecurityEntityTypes.Group);

            viewUsers.AddRange(viewGroups);
            viewUsers.Sort();
            model.ViewAccounts = string.Join("; ", viewUsers);

            modifyUsers.AddRange(modifyGroups);
            modifyUsers.Sort();
            model.ModifyAccounts = string.Join("; ", modifyUsers);
        }

        public string InsertArticle(string title, string text, IEnumerable<string> tags, IEnumerable<ArticleSecurity> security, string userId)
        {
            string urlTitle = Sluggifier.GenerateSlug(title);
            if (urlTitle.Length > 100)
            {
                urlTitle = urlTitle.Substring(0, 100);
            }

            // TODO: Ensure url title is unique
            string html = this.converter.Convert(text, urlTitle);

            using (var scope = new TransactionScope())
            {
                var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
                var cmd = connection.CreateCommand();
                connection.Open();

                try
                {
                    cmd.CommandText = "up_Article_Insert";
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter[] parameters = new SqlParameter[6];

                    parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
                    parameters[0].Direction = ParameterDirection.Output;

                    parameters[1] = new SqlParameter("@Title", SqlDbType.NVarChar, 100);
                    parameters[1].Value = title;

                    parameters[2] = new SqlParameter("@UrlTitle", SqlDbType.NVarChar, 100);
                    parameters[2].Value = urlTitle;

                    parameters[3] = new SqlParameter("@Text", SqlDbType.NVarChar, -1);
                    parameters[3].Value = text;

                    parameters[4] = new SqlParameter("@Html", SqlDbType.NVarChar, -1);
                    parameters[4].Value = html;

                    parameters[5] = new SqlParameter("@UpdatedBy", SqlDbType.NVarChar, 50);
                    parameters[5].Value = userId;

                    cmd.Parameters.AddRange(parameters);

                    cmd.ExecuteNonQuery();

                    int articleId = (int)parameters[0].Value;

                    cmd.CommandText = "up_ArticleTag_Insert";
                    cmd.Parameters.Clear();

                    parameters = new SqlParameter[2];

                    parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
                    parameters[0].Value = articleId;

                    parameters[1] = new SqlParameter("@Tag", SqlDbType.NVarChar, 50);
                    cmd.Parameters.AddRange(parameters);

                    foreach (var tag in tags)
                    {
                        parameters[1].Value = tag;
                        cmd.ExecuteNonQuery();
                    }

                    cmd.CommandText = "up_ArticleSecurity_Update";
                    cmd.Parameters.Clear();

                    parameters = new SqlParameter[4];

                    parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
                    parameters[0].Value = articleId;

                    parameters[1] = new SqlParameter("@Scope", SqlDbType.Char, 1);
                    parameters[2] = new SqlParameter("@EntityId", SqlDbType.NVarChar, 256);
                    parameters[3] = new SqlParameter("@Permission", SqlDbType.Char, 1);

                    cmd.Parameters.AddRange(parameters);

                    foreach (var securityRecord in security)
                    {
                        cmd.Parameters[1].Value = securityRecord.Scope;
                        cmd.Parameters[2].Value = securityRecord.EntityId ?? string.Empty;
                        cmd.Parameters[3].Value = securityRecord.Permission;
                        cmd.ExecuteNonQuery();
                    }

                    scope.Complete();
                }
                finally
                {
                    connection.Close();
                }
            }

            return urlTitle;
        }

        public void InsertArticleAttachment(int articleId, string filename, string title)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.up_ArticleAttachment_Insert";
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@Filename", filename);
                cmd.Parameters.AddWithValue("@Title", title);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertArticleImage(int articleId, string filename, string title)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.up_ArticleImage_Insert";
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@Filename", filename);
                cmd.Parameters.AddWithValue("@Title", title);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void MarkArticleViewed(int articleId, string userId)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.up_Article_View";
                cmd.Parameters.AddWithValue("@ArticleId", articleId);
                cmd.Parameters.AddWithValue("@ViewedBy", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public void ReviewArticle(int articleId, string userId)
        {
            Article article = this.context.Articles.SingleOrDefault(a => a.ArticleId == articleId);
            if (article == null)
            {
                throw new ArgumentException("Invalid article id", "articleId");
            }

            article.LastReviewedBy = userId;
            article.LastReviewedWhen = DateTime.Now;
            this.context.Entry(article).State = EntityState.Modified;
            this.context.SaveChanges();
        }

        public IEnumerable<ArticleSearchResult> SearchByQuery(string query, IIdentity identity)
        {
            var results = new List<ArticleSearchResult>();

            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "up_Article_SearchByQuery";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter[] parameters = new SqlParameter[3];

            parameters[0] = new SqlParameter("@Query", SqlDbType.NVarChar, 1000);
            parameters[0].Value = query;

            parameters[1] = new SqlParameter("@UserId", SqlDbType.NVarChar, 256);
            parameters[1].Value = this.securityRepository.StandardizeUserId(identity.Name);

            parameters[2] = new SqlParameter("@UserGroups", SqlDbType.Structured);
            parameters[2].TypeName = "dbo.SecurityEntityTable";
            parameters[2].Value = this.GetGroupParameter(identity);

            cmd.Parameters.AddRange(parameters);

            conn.Open();

            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    int urlTitleOrdinal = reader.GetOrdinal("UrlTitle");
                    int titleOrdinal = reader.GetOrdinal("Title");
                    int updatedByOrdinal = reader.GetOrdinal("UpdatedBy");
                    int updatedWhenOrdinal = reader.GetOrdinal("UpdatedWhen");

                    while (reader.Read())
                    {
                        results.Add(new ArticleSearchResult()
                        {
                            Title = reader.GetString(titleOrdinal),
                            UpdatedBy = reader.GetString(updatedByOrdinal),
                            UpdatedWhen = reader.GetDateTime(updatedWhenOrdinal),
                            UrlTitle = reader.GetString(urlTitleOrdinal)
                        });
                    }
                }
            }
            finally
            {
                conn.Close();
            }

            this.GetDisplayNames(results);

            return results;
        }

        public IEnumerable<ArticleSearchResult> SearchByTag(string tag, IIdentity identity)
        {
            var results = new List<ArticleSearchResult>();

            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "up_Article_SearchByTag";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter[] parameters = new SqlParameter[3];

            parameters[0] = new SqlParameter("@Tag", SqlDbType.NVarChar, 30);
            if (string.IsNullOrEmpty(tag))
            {
                parameters[0].Value = DBNull.Value;
            }
            else
            {
                parameters[0].Value = tag;
            }

            parameters[1] = new SqlParameter("@UserId", SqlDbType.NVarChar, 256);
            parameters[1].Value = this.securityRepository.StandardizeUserId(identity.Name);

            parameters[2] = new SqlParameter("@UserGroups", SqlDbType.Structured);
            parameters[2].TypeName = "dbo.SecurityEntityTable";
            parameters[2].Value = this.GetGroupParameter(identity);

            cmd.Parameters.AddRange(parameters);

            conn.Open();

            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    int urlTitleOrdinal = reader.GetOrdinal("UrlTitle");
                    int titleOrdinal = reader.GetOrdinal("Title");
                    int updatedByOrdinal = reader.GetOrdinal("UpdatedBy");
                    int updatedWhenOrdinal = reader.GetOrdinal("UpdatedWhen");

                    while (reader.Read())
                    {
                        results.Add(new ArticleSearchResult()
                        {
                            Title = reader.GetString(titleOrdinal),
                            UpdatedBy = reader.GetString(updatedByOrdinal),
                            UpdatedWhen = reader.GetDateTime(updatedWhenOrdinal),
                            UrlTitle = reader.GetString(urlTitleOrdinal)
                        });
                    }
                }
            }
            finally
            {
                conn.Close();
            }

            this.GetDisplayNames(results);

            return results;
        }

        public void UpdateArticle(int articleId, string title, string urlTitle, string text, string comment, IEnumerable<string> tags, IEnumerable<ArticleSecurity> security, string userId)
        {
            // Create diff from current revision. Insert history record.
            string currentText = this.context.Articles.Where(a => a.ArticleId == articleId).Select(a => a.Text).Single();
            var diff = new diff_match_patch();
            List<Patch> patches = diff.patch_make(text, currentText);
            string delta = diff.patch_toText(patches);
            bool invalidateSecurityCache = false;

            // Update existing record.
            using (var scope = new TransactionScope())
            {
                string html = this.converter.Convert(text, urlTitle);

                var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
                var cmd = conn.CreateCommand();

                conn.Open();
                try
                {
                    // Update main article information.
                    cmd.CommandText = "up_Article_Update";
                    cmd.CommandType = CommandType.StoredProcedure;

                    SqlParameter[] parameters = new SqlParameter[8];

                    parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
                    parameters[0].Value = articleId;

                    parameters[1] = new SqlParameter("@Title", SqlDbType.NVarChar, 100);
                    parameters[1].Value = title;

                    parameters[2] = new SqlParameter("@UrlTitle", SqlDbType.NVarChar, 100);
                    parameters[2].Value = urlTitle;

                    parameters[3] = new SqlParameter("@Text", SqlDbType.NVarChar, -1);
                    parameters[3].Value = text;

                    parameters[4] = new SqlParameter("@Html", SqlDbType.NVarChar, -1);
                    parameters[4].Value = html;

                    parameters[5] = new SqlParameter("@Delta", SqlDbType.NVarChar, -1);
                    parameters[5].Value = delta;

                    parameters[6] = new SqlParameter("@UpdatedBy", SqlDbType.NVarChar, 50);
                    parameters[6].Value = userId;

                    parameters[7] = new SqlParameter("@Comment", SqlDbType.NVarChar, 100);
                    parameters[7].Value = comment ?? string.Empty;

                    cmd.Parameters.AddRange(parameters);

                    cmd.ExecuteNonQuery();

                    // Update security.
                    cmd.CommandText = "SELECT [Scope], [EntityId] FROM [dbo].[ArticleSecurity] WHERE [ArticleId] = @ArticleId";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@ArticleId", articleId);

                    var recordsToDelete = new List<Tuple<string, string>>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Is this current entity part of the new security records?
                            if (!security.Any(s => (s.EntityId ?? string.Empty) == reader.GetString(1) && s.Scope == reader.GetString(0)))
                            {
                                // If not, remove it from the database.
                                recordsToDelete.Add(Tuple.Create(reader.GetString(0), reader.GetString(1)));
                            }
                        }
                    }

                    cmd.CommandText = "up_ArticleSecurity_Delete";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Clear();
                    parameters = new SqlParameter[3];

                    parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
                    parameters[0].Value = articleId;

                    parameters[1] = new SqlParameter("@Scope", SqlDbType.Char, 1);
                    parameters[2] = new SqlParameter("@EntityId", SqlDbType.NVarChar, 256);
                    cmd.Parameters.AddRange(parameters);

                    foreach (var record in recordsToDelete)
                    {
                        cmd.Parameters[1].Value = record.Item1;
                        cmd.Parameters[2].Value = record.Item2;
                        cmd.ExecuteNonQuery();
                        invalidateSecurityCache = true;
                    }

                    cmd.CommandText = "up_ArticleSecurity_Update";
                    cmd.Parameters.Clear();

                    parameters = new SqlParameter[4];

                    parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
                    parameters[0].Value = articleId;

                    parameters[1] = new SqlParameter("@Scope", SqlDbType.Char, 1);
                    parameters[2] = new SqlParameter("@EntityId", SqlDbType.NVarChar, 256);
                    parameters[3] = new SqlParameter("@Permission", SqlDbType.Char, 1);

                    cmd.Parameters.AddRange(parameters);

                    foreach (var securityRecord in security)
                    {
                        cmd.Parameters[1].Value = securityRecord.Scope;
                        cmd.Parameters[2].Value = securityRecord.EntityId ?? string.Empty;
                        cmd.Parameters[3].Value = securityRecord.Permission;
                        cmd.ExecuteNonQuery();
                        invalidateSecurityCache = true;
                    }

                    // Update tags.
                    cmd.CommandText = "SELECT [Tag] FROM [dbo].[ArticleTag] WHERE [ArticleId] = @ArticleId";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@ArticleId", articleId);

                    var existingTags = new List<string>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            existingTags.Add(reader.GetString(0));
                        }
                    }

                    var tagsToDelete = existingTags.Except(tags, StringComparer.CurrentCultureIgnoreCase);
                    cmd.CommandText = "up_ArticleTag_Delete";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Clear();
                    parameters = new SqlParameter[2];

                    parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
                    parameters[0].Value = articleId;

                    parameters[1] = new SqlParameter("@Tag", SqlDbType.NVarChar, 30);
                    cmd.Parameters.AddRange(parameters);

                    foreach (var tag in tagsToDelete)
                    {
                        cmd.Parameters[1].Value = tag;
                        cmd.ExecuteNonQuery();
                    }

                    var tagsToInsert = tags.Except(existingTags, StringComparer.CurrentCultureIgnoreCase);
                    cmd.CommandText = "up_ArticleTag_Insert";
                    cmd.Parameters.Clear();

                    parameters = new SqlParameter[2];

                    parameters[0] = new SqlParameter("@ArticleId", SqlDbType.Int);
                    parameters[0].Value = articleId;

                    parameters[1] = new SqlParameter("@Tag", SqlDbType.NVarChar, 30);
                    cmd.Parameters.AddRange(parameters);

                    foreach (var tag in tagsToInsert)
                    {
                        parameters[1].Value = tag;
                        cmd.ExecuteNonQuery();
                    }

                    scope.Complete();
                }
                finally
                {
                    conn.Close();
                }

                if (invalidateSecurityCache)
                {
                    IEnumerable<ArticleSecurity> dummy;
                    ArticleSecurityCache.TryRemove(articleId, out dummy);
                }
            }
        }

        private static int GetScopeOrder(string scope)
        {
            // Minor effeciency; sort items in the records in the order from least to most expensive
            // to process.
            if (scope == Domain.ArticleSecurity.ScopeEveryone)
            {
                return 0;
            }

            if (scope == Domain.ArticleSecurity.ScopeIndividual)
            {
                return 1;
            }

            if (scope == Domain.ArticleSecurity.ScopeGroup)
            {
                return 2;
            }

            Debug.Fail("Invalid scope : " + scope);
            return int.MaxValue;
        }

        private void GetDisplayNames(List<ArticleSearchResult> results)
        {
            results.ForEach(delegate(ArticleSearchResult sr)
            {
                if (!string.IsNullOrEmpty(sr.UpdatedBy))
                {
                    var entity = this.securityRepository.Find(sr.UpdatedBy, SecurityEntityTypes.User);
                    if (entity != null)
                    {
                        sr.UpdatedByDisplayName = entity.Name;
                    }
                }
            });
        }

        private object GetGroupParameter(IIdentity identity)
        {
            var windowsIdentity = identity as WindowsIdentity;

            if (windowsIdentity == null)
            {
                return DBNull.Value;
            }

            DataTable groups = new DataTable();
            groups.Columns.Add("EntityId", typeof(string));

            foreach (var g in windowsIdentity.Groups.Translate(typeof(NTAccount)))
            {
                if (g.Value.StartsWith(string.Format("{0}\\", SecurityDomain)))
                {
                    groups.Rows.Add(this.securityRepository.StandardizeUserId(g.Value));
                }
            }

            return groups;
        }

        private IEnumerable<ArticleSecurity> GetSecurityRecords(int articleId)
        {
            IEnumerable<ArticleSecurity> security;
            if (!ArticleSecurityCache.TryGetValue(articleId, out security))
            {
                List<ArticleSecurity> temp = this.ArticleSecurity.Where(s => s.ArticleId == articleId).ToList();
                temp.Sort((s1, s2) => GetScopeOrder(s1.Scope).CompareTo(GetScopeOrder(s2.Scope)));
                ArticleSecurityCache.TryAdd(articleId, temp);
                security = temp;
            }

            return security;
        }

        private void StandardizeAccounts(List<string> accounts, SecurityEntityTypes type)
        {
            accounts.RemoveAll(s => string.IsNullOrEmpty(s));

            for (int i = 0; i < accounts.Count; i++)
            {
                SecurityEntity entity = this.securityRepository.Find(accounts[i], type);
                if (entity != null)
                {
                    accounts[i] = entity.ToString();
                }
            }
        }
    }
}
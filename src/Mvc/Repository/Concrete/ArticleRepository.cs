namespace Otter.Repository
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.DirectoryServices;
    using System.Linq;
    using System.Transactions;
    using DiffMatchPatch;
    using Otter.Domain;
    using Otter.Infrastructure;
    using Otter.Models;
    using Otter.Repository.Abstract;
    using System;

    public sealed class ArticleRepository : IArticleRepository
    {
        private readonly IApplicationDbContext context;
        private readonly ITextToHtmlConverter converter;
        private readonly ISecurityRepository securityRepository;

        public ArticleRepository(IApplicationDbContext context, ITextToHtmlConverter converter, ISecurityRepository securityRepository)
        {
            this.context = context;
            this.converter = converter;
            this.securityRepository = securityRepository;
        }

        public IQueryable<Article> Articles
        {
            get { return this.context.Articles; }
        }

        public IQueryable<ArticleHistory> ArticleHistory
        {
            get { return this.context.ArticleHistory; }
        }

        public IQueryable<ArticleSecurity> ArticleSecurity
        {
            get { return this.context.ArticleSecurity; }
        }

        public IQueryable<ArticleTag> ArticleTags
        {
            get { return this.context.ArticleTags; }
        }

        public string InsertArticle(string title, string text, IEnumerable<string> tags, string userId)
        {
            string urlTitle = Sluggifier.GenerateSlug(title);
            if (urlTitle.Length > 100)
            {
                urlTitle = urlTitle.Substring(0, 100);
            }

            // TODO: Ensure url title is unique

            string html = this.converter.Convert(text);

            var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = connection.CreateCommand();

            connection.Open();

            try
            {
                using (var scope = new TransactionScope())
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

                    scope.Complete();
                }
            }
            finally
            {
                connection.Close();
            }

            return urlTitle;
        }

        public void UpdateArticle(int articleId, string title, string urlTitle, string text, string comment, IEnumerable<ArticleSecurity> security, string userId)
        {
            // Create diff from current revision. Insert history record.
            string currentText = this.context.Articles.Where(a => a.ArticleId == articleId).Select(a => a.Text).Single();
            var diff = new diff_match_patch();
            List<Patch> patches = diff.patch_make(text, currentText);
            string delta = diff.patch_toText(patches);

            // Update existing record.
            string html = this.converter.Convert(text);

            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = conn.CreateCommand();

            conn.Open();
            try
            {
                using (var scope = new TransactionScope())
                {
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

                    cmd.CommandText = "SELECT [Scope], [EntityId] FROM [dbo].[ArticleSecurity] WHERE [ArticleId] = @ArticleId";
                    cmd.CommandType = CommandType.Text;
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@ArticleId", articleId);

                    var recordsToDelete = new List<Tuple<string, string>>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!security.Any(s => s.EntityId == reader.GetString(1) && s.Scope == reader.GetString(0)))
                            {
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
            }
            finally
            {
                conn.Close();
            }
        }

        public string GetRevisionHtml(int articleId, int revision)
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

            return this.converter.Convert(text);
        }

        public IEnumerable<ArticleSearchResult> Search(string query, string userId)
        {
            var results = new List<ArticleSearchResult>();

            var conn = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = conn.CreateCommand();
            cmd.CommandText = "up_Article_Search";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter[] parameters = new SqlParameter[2];

            parameters[0] = new SqlParameter("@Query", SqlDbType.NVarChar, 1000);
            parameters[0].Value = query;

            parameters[1] = new SqlParameter("@UserId", SqlDbType.NVarChar, 50);
            parameters[1].Value = userId;

            cmd.Parameters.AddRange(parameters);

            conn.Open();

            try
            {
                using (var reader = cmd.ExecuteReader())
                {
                    int urlTitleOrdinal = reader.GetOrdinal("UrlTitle");
                    int titleOrdinal = reader.GetOrdinal("Title");
                    int updatedByOrdinal = reader.GetOrdinal("UpdatedBy");
                    int updatedDtmOrdinal = reader.GetOrdinal("UpdatedDtm");

                    while (reader.Read())
                    {
                        results.Add(new ArticleSearchResult()
                        {
                            Title = reader.GetString(titleOrdinal),
                            UpdatedBy = reader.GetString(updatedByOrdinal),
                            UpdatedDtm = reader.GetDateTime(updatedDtmOrdinal),
                            UrlTitle = reader.GetString(urlTitleOrdinal)
                        });
                    }
                }
            }
            finally
            {
                conn.Close();
            }

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

            return results;
        }

        public void HydratePermissionModel(PermissionModel model, int articleId, string userId)
        {
            // Assume that permissions are specified groups and individuals. If we find an "Everyone" scope in the
            // security tokens, we will modify this.
            model.ModifyOption = PermissionOption.Specified;
            model.ViewOption = PermissionOption.Specified;

            var viewGroups = new List<string>();
            var viewUsers = new List<string>();
            var modifyGroups = new List<string>();
            var modifyUsers = new List<string>();

            var tokens = this.ArticleSecurity.Where(s => s.ArticleId == articleId);
            foreach (var token in tokens)
            {
                // If this is a view token and we've already learned that everyone can view, there's no need to process the record.
                if (model.ViewOption == PermissionOption.Everyone && token.Permission == Otter.Domain.ArticleSecurity.PermissionView)
                {
                    continue;
                }

                // If this is a modify token and we've already learned that everyone can modify, there's no need to process the record.
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

            viewUsers.RemoveAll(s => string.IsNullOrEmpty(s));
            viewGroups.RemoveAll(s => string.IsNullOrEmpty(s));
            modifyUsers.RemoveAll(s => string.IsNullOrEmpty(s));
            modifyGroups.RemoveAll(s => string.IsNullOrEmpty(s));

            using (var searcher = new DirectorySearcher())
            {
                for (int i = 0; i < viewUsers.Count; i++)
                {
                    searcher.Filter = string.Format("(&(objectCategory=person)(objectClass=user)(sAMAccountName={0}))", viewUsers[i]);
                    SearchResult result = searcher.FindOne();
                    if (result != null)
                    {
                        viewUsers[i] = string.Format("{0} [{1}]", result.Properties["displayName"][0], result.Properties["sAMAccountName"][0]);
                    }
                }

                for (int i = 0; i < modifyUsers.Count; i++)
                {
                    searcher.Filter = string.Format("(&(objectCategory=person)(objectClass=user)(sAMAccountName={0}))", modifyUsers[i]);
                    SearchResult result = searcher.FindOne();
                    if (result != null)
                    {
                        modifyUsers[i] = string.Format("{0} [{1}]", result.Properties["displayName"][0], result.Properties["sAMAccountName"][0]);
                    }
                }

                for (int i = 0; i < viewGroups.Count; i++)
                {
                    searcher.Filter = string.Format("(&(objectCategory=group)(cn={0}*))", viewGroups[i]);
                    SearchResult result = searcher.FindOne();
                    if (result != null)
                    {
                        viewGroups[i] = string.Format("{0} (Group)", result.Properties["cn"][0]);
                    }
                }

                for (int i = 0; i < modifyGroups.Count; i++)
                {
                    searcher.Filter = string.Format("(&(objectCategory=group)(cn={0}*))", modifyGroups[i]);
                    SearchResult result = searcher.FindOne();
                    if (result != null)
                    {
                        modifyGroups[i] = string.Format("{0} (Group)", result.Properties["cn"][0]);
                    }
                }
            }

            viewUsers.AddRange(viewGroups);
            viewUsers.Sort();
            model.ViewAccounts = string.Join("; ", viewUsers);

            modifyUsers.AddRange(modifyGroups);
            modifyUsers.Sort();
            model.ModifyAccounts = string.Join("; ", modifyUsers);
        }
    }
}
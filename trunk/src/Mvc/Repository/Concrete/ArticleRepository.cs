namespace Otter.Repository
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using DiffMatchPatch;
    using Otter.Domain;
    using Otter.Infrastructure;
    using Otter.Repository.Abstract;
    using System.Transactions;

    public sealed class ArticleRepository : IArticleRepository
    {
        private readonly IApplicationDbContext context;
        private readonly ITextToHtmlConverter converter;

        public ArticleRepository(IApplicationDbContext context, ITextToHtmlConverter converter)
        {
            this.context = context;
            this.converter = converter;
        }

        public IQueryable<Article> Articles
        {
            get { return this.context.Articles; }
        }

        public IQueryable<ArticleHistory> ArticleHistory
        {
            get { return this.context.ArticleHistory; }
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

        public void UpdateArticle(int articleId, string title, string urlTitle, string text, string comment, string userId)
        {
            // Create diff from current revision. Insert history record.
            string currentText = this.context.Articles.Where(a => a.ArticleId == articleId).Select(a => a.Text).Single();
            var diff = new diff_match_patch();
            List<Patch> patches = diff.patch_make(text, currentText);
            string delta = diff.patch_toText(patches);

            // Update existing record.
            string html = this.converter.Convert(text);

            var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = connection.CreateCommand();
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

            connection.Open();

            try
            {
                cmd.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
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
   }
}
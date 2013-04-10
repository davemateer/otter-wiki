namespace Otter.Mvc.Repository
{
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Security.Cryptography;
    using Otter.Infrastructure;
    using Otter.Mvc.Domain;
    using Otter.Mvc.Infrastructure;
    using Otter.Mvc.Repository.Abstract;
    using System.Collections.Generic;

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

        public string InsertArticle(string title, string text)
        {
            string urlTitle = Sluggifier.GenerateSlug(title);
            if (urlTitle.Length > 100)
            {
                urlTitle = urlTitle.Substring(0, 100);
            }

            // TODO: Ensure url title is unique

            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] hash = md5.ComputeHash(inputBytes);
            string html = this.converter.Convert(text);

            var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["Otter"].ConnectionString);
            var cmd = connection.CreateCommand();
            cmd.CommandText = "up_Article_Insert";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter[] parameters = new SqlParameter[6];

            parameters[0] = new SqlParameter("@Title", SqlDbType.NVarChar, 100);
            parameters[0].Value = title;

            parameters[1] = new SqlParameter("@UrlTitle", SqlDbType.NVarChar, 100);
            parameters[1].Value = urlTitle;

            parameters[2] = new SqlParameter("@Text", SqlDbType.NVarChar, -1);
            parameters[2].Value = text;

            parameters[3] = new SqlParameter("@TextHash", SqlDbType.Binary, 16);
            parameters[3].Value = hash;

            parameters[4] = new SqlParameter("@Html", SqlDbType.NVarChar, -1);
            parameters[4].Value = html;

            parameters[5] = new SqlParameter("@LastUpdatedBy", SqlDbType.NVarChar, 50);
            parameters[5].Value = "TODO";

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

            return urlTitle;
        }

        public void UpdateArticle(int articleId, string title, string urlTitle, string text)
        {
            // Create diff from current revision. Insert history record.
            string currentText = this.context.Articles.Where(a => a.ArticleId == articleId).Select(a => a.Text).Single();

            // Update existing record.
            var diff = new DiffMatchPatch.diff_match_patch();
            List<DiffMatchPatch.Patch> patches = diff.patch_make(text, currentText);
            string delta = diff.patch_toText(patches);
        }
    }
}
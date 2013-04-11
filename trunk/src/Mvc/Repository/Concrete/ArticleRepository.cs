﻿namespace Otter.Mvc.Repository
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Linq;
    using System.Security.Cryptography;
    using DiffMatchPatch;
    using Otter.Infrastructure;
    using Otter.Mvc.Domain;
    using Otter.Mvc.Infrastructure;
    using Otter.Mvc.Repository.Abstract;

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

        public string InsertArticle(string title, string text, string userId)
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
            cmd.CommandText = "up_Article_Insert";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter[] parameters = new SqlParameter[5];

            parameters[0] = new SqlParameter("@Title", SqlDbType.NVarChar, 100);
            parameters[0].Value = title;

            parameters[1] = new SqlParameter("@UrlTitle", SqlDbType.NVarChar, 100);
            parameters[1].Value = urlTitle;

            parameters[2] = new SqlParameter("@Text", SqlDbType.NVarChar, -1);
            parameters[2].Value = text;

            parameters[3] = new SqlParameter("@Html", SqlDbType.NVarChar, -1);
            parameters[3].Value = html;

            parameters[4] = new SqlParameter("@UpdatedBy", SqlDbType.NVarChar, 50);
            parameters[4].Value = userId;

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
    }
}
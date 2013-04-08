namespace Otter.Mvc.Repository
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using Otter.Mvc.Infrastructure;
    using System.Security.Cryptography;
    using System.Data.SqlClient;
using System.Data;
    using System.Configuration;
    using Otter.Mvc.Repository.Abstract;
    using Otter.Mvc.Domain;

    public sealed class ArticleRepository : IArticleRepository
    {
        private readonly IApplicationDbContext context;

        public ArticleRepository(IApplicationDbContext context)
        {
            this.context = context;
        }

        public IQueryable<Article> Articles
        {
            get { return this.context.Articles; }
        }

        public string InsertArticle(string title, string text)
        {
            string urlFriendlyTitle = Sluggifier.GenerateSlug(title);
            if (urlFriendlyTitle.Length > 100)
            {
                urlFriendlyTitle = urlFriendlyTitle.Substring(0, 100);
            }

            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] hash = md5.ComputeHash(inputBytes);

            var connection = new SqlConnection(ConfigurationManager.ConnectionStrings["ThisApplication"].ConnectionString);
            var cmd = connection.CreateCommand();
            cmd.CommandText = "up_Article_Insert";
            cmd.CommandType = CommandType.StoredProcedure;

            SqlParameter[] parameters = new SqlParameter[5];

            parameters[0] = new SqlParameter("@Title", SqlDbType.NVarChar, 100);
            parameters[0].Value = title;

            parameters[1] = new SqlParameter("@UrlFriendlyTitle", SqlDbType.NVarChar, 100);
            parameters[1].Value = urlFriendlyTitle;

            parameters[2] = new SqlParameter("@Text", SqlDbType.NVarChar, -1);
            parameters[2].Value = text;

            parameters[3] = new SqlParameter("@TextHash", SqlDbType.Binary, 16);
            parameters[3].Value = hash;

            parameters[4] = new SqlParameter("@LastUpdatedBy", SqlDbType.NVarChar, 50);
            parameters[4].Value = "TODO";

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

            return urlFriendlyTitle;
        }
    }
}
//-----------------------------------------------------------------------
// <copyright file="ArticleContentAuthorizationModule.cs" company="Dave Mateer">
// The MIT License (MIT)
//
// Copyright (c) 2015 Dave Mateer
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
namespace Otter
{
    using System;
    using System.Configuration;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Mvc;
    using Otter.Repository;

    public sealed class ArticleContentAuthorizationModule : IHttpModule
    {
        private static readonly string ArticleImageVirtualDirectory = ConfigurationManager.AppSettings["otter:ArticleImageVirtualDirectory"];
        private static readonly Regex ArticlePattern = new Regex(string.Format(@"~/{0}/(?<articleid>[^/]+)/", Regex.Escape(ConfigurationManager.AppSettings["otter:ArticleImageVirtualDirectory"])), RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.PostAuthorizeRequest -= this.OnPostAuthorizeRequest;
            context.PostAuthorizeRequest += this.OnPostAuthorizeRequest;
        }

        private void OnPostAuthorizeRequest(object sender, EventArgs e)
        {
            HttpApplication httpApplication = sender as HttpApplication;
            if (httpApplication == null || httpApplication.Context == null || httpApplication.Context.Request == null)
            {
                return;
            }

            string appFilePath = httpApplication.Context.Request.AppRelativeCurrentExecutionFilePath;
            if (appFilePath.StartsWith(string.Format("~/{0}/", ArticleImageVirtualDirectory), StringComparison.OrdinalIgnoreCase))
            {
                bool authorized = false;

                if (httpApplication.Context.User != null && httpApplication.Context.User.Identity != null && httpApplication.Context.User.Identity.IsAuthenticated)
                {
                    Match match = ArticlePattern.Match(appFilePath);
                    if (match.Success)
                    {
                        IArticleRepository articleRepository = DependencyResolver.Current.GetService<IArticleRepository>();
                        string articleId = match.Groups["articleid"].Value;
                        var article = articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == articleId);
                        if (article != null && articleRepository.CanView(httpApplication.Context.User, article.ArticleId))
                        {
                            authorized = true;
                        }
                    }
                }

                if (!authorized)
                {
                    throw new HttpException(401, "Access denied");
                }
            }
        }
    }
}
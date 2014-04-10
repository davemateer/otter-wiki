//-----------------------------------------------------------------------
// <copyright file="IArticleRepository.cs" company="Dave Mateer">
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
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Principal;
    using Otter.Domain;
    using Otter.Models;

    public interface IArticleRepository
    {
        IQueryable<Article> Articles { get; }

        IQueryable<ArticleHistory> ArticleHistory { get; }

        IQueryable<ArticleTag> ArticleTags { get; }

        IQueryable<ArticleSecurity> ArticleSecurity { get; }

        string InsertArticle(string title, string text, IEnumerable<string> tags, IEnumerable<ArticleSecurity> security, string userId);

        void UpdateArticle(int articleId, string title, string urlTitle, string text, string comment, IEnumerable<string> tags, IEnumerable<ArticleSecurity> security, string userId);

        string GetRevisionHtml(int articleId, int revision);

        string GetRevisionText(int articleId, int revision);

        IEnumerable<ArticleSearchResult> SearchByQuery(string query, IIdentity identity);

        IEnumerable<ArticleSearchResult> SearchByTag(string tag, IIdentity identity);

        IEnumerable<Tuple<string, int>> GetTagSummary(IIdentity identity);

        void HydratePermissionModel(PermissionModel model, int articleId, string userId);

        bool CanView(IPrincipal user, int articleId);

        bool CanModify(IPrincipal user, int articleId);
    }
}

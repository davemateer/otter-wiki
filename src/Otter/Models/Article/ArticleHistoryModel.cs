//-----------------------------------------------------------------------
// <copyright file="ArticleHistoryModel.cs" company="Dave Mateer">
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
namespace Otter.Models
{
    using System;
    using System.Collections.Generic;
    using AutoMapper;

    public sealed class ArticleHistoryModel : IUpdatedArticle
    {
        public int ArticleId { get; set; }

        public string Comment { get; set; }

        [IgnoreMap]
        public IEnumerable<ArticleHistoryRecord> HistoryRecords { get; set; }

        public int Revision { get; set; }

        public string Title { get; set; }

        public string UpdatedBy { get; set; }

        [IgnoreMap]
        public string UpdatedByDisplayName { get; set; }

        public DateTime UpdatedWhen { get; set; }

        public string UrlTitle { get; set; }
    }
}
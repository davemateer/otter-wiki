//-----------------------------------------------------------------------
// <copyright file="AutomapperConfiguration.cs" company="Dave Mateer">
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
namespace Otter.Infrastructure
{
    using AutoMapper;
    using Otter.Domain;
    using Otter.Models;

    public static class AutomapperConfiguration
    {
        public static void Configure()
        {
            Mapper.CreateMap<Article, ArticleReadModel>();
            Mapper.CreateMap<Article, ArticleEditModel>();
            Mapper.CreateMap<Article, ArticleHistoryModel>();
            Mapper.CreateMap<Article, ArticleSearchResult>();

            Mapper.CreateMap<ArticleHistory, ArticleReadModel>()
                .ForMember(m => m.UrlTitle, opt => opt.Ignore())
                .ForMember(m => m.Html, opt => opt.Ignore());

            Mapper.CreateMap<ArticleBase, ArticleCompareRecord>();

            Mapper.AssertConfigurationIsValid();
        }
    }
}
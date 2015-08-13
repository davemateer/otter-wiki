//-----------------------------------------------------------------------
// <copyright file="LuceneSearchProvider.cs" company="Dave Mateer">
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
namespace Otter.Repository
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Security.Principal;
    using System.Web.Hosting;
    using HtmlAgilityPack;
    using Lucene.Net.Analysis;
    using Lucene.Net.Analysis.Standard;
    using Lucene.Net.Documents;
    using Lucene.Net.Index;
    using Lucene.Net.QueryParsers;
    using Lucene.Net.Search;
    using Lucene.Net.Search.Highlight;
    using Lucene.Net.Store;
    using Otter.Domain;
    using IODirectory = System.IO.Directory;
    using LuceneDirectory = Lucene.Net.Store.Directory;

    public sealed class LuceneSearchProvider : ISearchProvider
    {
        private const int HighlightFragmentCount = 3;
        private const int HighlightFragmentLength = 80;
        private const string HighlightFragmentSeparator = "...";
        private const string IndexFieldArticleId = "id";
        private const string IndexFieldSecurityAll = "security.all";
        private const string IndexFieldSecurityAllValue = "1";
        private const string IndexFieldSecurityGroup = "security.group";
        private const string IndexFieldSecurityUser = "security.user";
        private const string IndexFieldTagNone = "tag.none";
        private const string IndexFieldTagNoneValue = "1";
        private const string IndexFieldTagTerm = "tag.term";
        private const string IndexFieldTagText = "tag.text";
        private const string IndexFieldText = "text";
        private const string IndexFieldTitle = "title";
        private const string IndexFieldUpdatedBy = "updatedBy";
        private const string IndexFieldUpdatedWhen = "updatedWhen";
        private const string IndexFieldUrlTitle = "urlTitle";
        private const Lucene.Net.Util.Version LuceneVersion = Lucene.Net.Util.Version.LUCENE_30;
        private const int MaxSearchResultCount = 100;
        private static readonly string LuceneIndexDirectory;
        private readonly IArticleRepository articleRepository;
        private readonly ISecurityRepository securityRepository;

        static LuceneSearchProvider()
        {
            string indexDirectory = ConfigurationManager.AppSettings["otter:LuceneIndexDirectory"];
            if (Path.IsPathRooted(indexDirectory))
            {
                LuceneIndexDirectory = indexDirectory;
            }
            else
            {
                LuceneIndexDirectory = Path.Combine(HostingEnvironment.ApplicationPhysicalPath, indexDirectory);
            }

            if (!IODirectory.Exists(LuceneIndexDirectory))
            {
                IODirectory.CreateDirectory(LuceneIndexDirectory);
            }
        }

        public LuceneSearchProvider(ISecurityRepository securityRepository, IArticleRepository articleRepository)
        {
            this.securityRepository = securityRepository;
            this.articleRepository = articleRepository;
        }

        public void RebuildIndex()
        {
            List<string> titles = new List<string>();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["otter"].ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "SELECT UrlTitle FROM dbo.Article";
                cmd.CommandType = CommandType.Text;
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        titles.Add(reader.GetString(0));
                    }
                }
            }

            using (StandardAnalyzer analyzer = new StandardAnalyzer(LuceneVersion))
            using (LuceneDirectory directory = FSDirectory.Open(LuceneIndexDirectory))
            using (IndexWriter writer = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                foreach (string title in titles)
                {
                    this.UpdateIndexImpl(title, writer);
                }

                writer.Optimize();
            }
        }

        public IEnumerable<ArticleSearchResult> SearchByQuery(string query, IIdentity identity)
        {
            using (StandardAnalyzer analyzer = new StandardAnalyzer(LuceneVersion))
            using (LuceneDirectory directory = FSDirectory.Open(LuceneIndexDirectory))
            using (IndexSearcher searcher = new IndexSearcher(directory, true))
            {
                MultiFieldQueryParser parser = new MultiFieldQueryParser(LuceneVersion, new[] { IndexFieldText, IndexFieldTitle, IndexFieldTagText }, analyzer);
                Query luceneQuery = parser.Parse(query);
                List<ArticleSearchResult> results = this.Search(searcher, luceneQuery, identity, MaxSearchResultCount);
                return results;
            }
        }

        public IEnumerable<ArticleSearchResult> SearchByTag(string tag, IIdentity identity)
        {
            using (LuceneDirectory directory = FSDirectory.Open(LuceneIndexDirectory))
            using (IndexSearcher searcher = new IndexSearcher(directory, true))
            {
                TermQuery luceneQuery = string.IsNullOrEmpty(tag) ? new TermQuery(new Term(IndexFieldTagNone, IndexFieldTagNoneValue)) : new TermQuery(new Term(IndexFieldTagTerm, tag));
                List<ArticleSearchResult> results = this.Search(searcher, luceneQuery, identity, int.MaxValue);
                return results;
            }
        }

        public void UpdateIndex(string articleUrlTitle)
        {
            using (StandardAnalyzer analyzer = new StandardAnalyzer(LuceneVersion))
            using (LuceneDirectory directory = FSDirectory.Open(LuceneIndexDirectory))
            using (IndexWriter writer = new IndexWriter(directory, analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED))
            {
                Term term = new Term(IndexFieldUrlTitle, articleUrlTitle);
                writer.DeleteDocuments(term);
                this.UpdateIndexImpl(articleUrlTitle, writer);
                writer.Optimize();
            }
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

        private List<ArticleSearchResult> Search(IndexSearcher searcher, Query query, IIdentity identity, int maxResultCount)
        {
            // Create filter to restrict results by security settings. For a document to be visible,
            // either (1) Everyone has access to the document, or (2) the user is included in the
            // list of allowed users, or (3) at least one group of which the user is a member is in
            // the list of allowed groups.
            BooleanQuery securityQuery = new BooleanQuery();
            securityQuery.Add(new TermQuery(new Term(IndexFieldSecurityAll, IndexFieldSecurityAllValue)), Occur.SHOULD);
            securityQuery.Add(new TermQuery(new Term(IndexFieldSecurityUser, this.securityRepository.StandardizeUserId(identity.Name))), Occur.SHOULD);
            foreach (var group in this.securityRepository.GetSecurityGroups(identity))
            {
                securityQuery.Add(new TermQuery(new Term(IndexFieldSecurityGroup, group)), Occur.SHOULD);
            }

            QueryWrapperFilter filter = new QueryWrapperFilter(securityQuery);

            // Perform the search against the Lucene index.
            TopDocs hits = searcher.Search(query, filter, maxResultCount);

            // Build the objects necessary for hit highlighting.
            SimpleHTMLFormatter highlightFormatter = new SimpleHTMLFormatter();
            QueryScorer highlightScorer = new QueryScorer(query);
            Highlighter highlighter = new Highlighter(highlightFormatter, highlightScorer);
            highlighter.TextFragmenter = new SimpleFragmenter(HighlightFragmentLength);
            StandardAnalyzer highlightAnalyzer = new StandardAnalyzer(LuceneVersion);

            List<ArticleSearchResult> results = new List<ArticleSearchResult>(hits.TotalHits);

            foreach (ScoreDoc item in hits.ScoreDocs)
            {
                Document doc = searcher.Doc(item.Doc);
                string text = doc.Get(IndexFieldText);
                TokenStream stream = highlightAnalyzer.TokenStream(string.Empty, new StringReader(text));
                string sample = highlighter.GetBestFragments(stream, text, HighlightFragmentCount, HighlightFragmentSeparator);

                ArticleSearchResult result = new ArticleSearchResult
                {
                    FragmentHtml = sample,
                    Title = doc.Get(IndexFieldTitle),
                    UpdatedBy = doc.Get(IndexFieldUpdatedBy),
                    UpdatedWhen = DateTools.StringToDate(doc.Get(IndexFieldUpdatedWhen)),
                    UrlTitle = doc.Get(IndexFieldUrlTitle)
                };

                results.Add(result);
            }

            this.GetDisplayNames(results);
            return results;
        }

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Ordinal notation is acceptable.")]
        private void UpdateIndexImpl(string articleUrlTitle, IndexWriter writer)
        {
            int articleId = 0;
            Document doc = new Document();

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["otter"].ConnectionString))
            using (SqlCommand cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.CommandText = "SELECT ArticleId, Title, UrlTitle, UpdatedBy, UpdatedWhen, Html FROM dbo.Article WHERE UrlTitle = @UrlTitle";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.AddWithValue("@UrlTitle", articleUrlTitle);

                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SingleRow))
                {
                    int oArticleId = reader.GetOrdinal("ArticleId");
                    int oHtml = reader.GetOrdinal("Html");
                    int oTitle = reader.GetOrdinal("Title");
                    int oUpdatedBy = reader.GetOrdinal("UpdatedBy");
                    int oUpdatedWhen = reader.GetOrdinal("UpdatedWhen");
                    int oUrlTitle = reader.GetOrdinal("UrlTitle");

                    if (reader.Read())
                    {
                        HtmlDocument htmlDocument = new HtmlDocument();
                        htmlDocument.LoadHtml(reader.GetString(oHtml));
                        string innerText = htmlDocument.DocumentNode.InnerText;

                        articleId = reader.GetInt32(oArticleId);
                        doc.Add(new Field(IndexFieldArticleId, articleId.ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                        doc.Add(new Field(IndexFieldTitle, reader.GetString(oTitle), Field.Store.YES, Field.Index.ANALYZED));
                        doc.Add(new Field(IndexFieldUrlTitle, reader.GetString(oUrlTitle), Field.Store.YES, Field.Index.NOT_ANALYZED));
                        doc.Add(new Field(IndexFieldUpdatedBy, reader.GetString(oUpdatedBy), Field.Store.YES, Field.Index.NO));
                        doc.Add(new Field(IndexFieldUpdatedWhen, DateTools.DateToString(reader.GetDateTime(oUpdatedWhen), DateTools.Resolution.SECOND), Field.Store.YES, Field.Index.NO));
                        doc.Add(new Field(IndexFieldText, innerText, Field.Store.YES, Field.Index.ANALYZED));
                    }
                    else
                    {
                        return;
                    }
                }

                bool tagFound = false;
                cmd.CommandText = "SELECT Tag FROM dbo.ArticleTag WHERE ArticleId = @ArticleId";
                cmd.CommandType = CommandType.Text;
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@ArticleId", articleId);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tagFound = true;
                        doc.Add(new Field(IndexFieldTagTerm, reader.GetString(0), Field.Store.YES, Field.Index.NOT_ANALYZED));
                        doc.Add(new Field(IndexFieldTagText, reader.GetString(0), Field.Store.YES, Field.Index.ANALYZED));
                    }
                }

                if (!tagFound)
                {
                    doc.Add(new Field(IndexFieldTagNone, IndexFieldTagNoneValue, Field.Store.YES, Field.Index.NOT_ANALYZED));
                }
            }

            IEnumerable<ArticleSecurity> security = this.articleRepository.GetSecurityRecords(articleId);
            if (security.Any(s => s.Scope == ArticleSecurity.ScopeEveryone))
            {
                doc.Add(new Field(IndexFieldSecurityAll, IndexFieldSecurityAllValue, Field.Store.YES, Field.Index.NOT_ANALYZED));
            }
            else
            {
                IEnumerable<string> groups = security.Where(s => s.Scope == ArticleSecurity.ScopeGroup).Select(s => s.EntityId).Distinct();
                foreach (string group in groups)
                {
                    doc.Add(new Field(IndexFieldSecurityGroup, group, Field.Store.YES, Field.Index.NOT_ANALYZED));
                }

                IEnumerable<string> users = security.Where(s => s.Scope == ArticleSecurity.ScopeIndividual).Select(s => s.EntityId).Distinct();
                foreach (string user in users)
                {
                    doc.Add(new Field(IndexFieldSecurityUser, user, Field.Store.YES, Field.Index.NOT_ANALYZED));
                }
            }

            writer.AddDocument(doc);
        }
    }
}
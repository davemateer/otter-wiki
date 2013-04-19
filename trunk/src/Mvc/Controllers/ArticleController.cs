namespace Otter.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web.Mvc;
    using AutoMapper;
    using Otter.Infrastructure;
    using Otter.Models;
    using Otter.Repository;

    public class ArticleController : Controller
    {
        private readonly IArticleRepository articleRepository;
        private readonly ITextToHtmlConverter htmlConverter;

        public ArticleController(IArticleRepository articleRepository, ITextToHtmlConverter htmlConverter)
        {
            this.articleRepository = articleRepository;
            this.htmlConverter = htmlConverter;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Read(string id, int? revision)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return HttpNotFound();
            }

            ArticleReadModel model = null;

            if (revision.HasValue && revision.Value != article.Revision)
            {
                var articleRevision = this.articleRepository.ArticleHistory.FirstOrDefault(h => h.ArticleId == article.ArticleId && h.Revision == revision.Value);
                if (articleRevision == null)
                {
                    return HttpNotFound();
                }

                model = Mapper.Map<ArticleReadModel>(articleRevision);
                model.Html = articleRevision.Text == null ? this.articleRepository.GetRevisionHtml(articleRevision.ArticleId, articleRevision.Revision) : this.htmlConverter.Convert(articleRevision.Text);
                model.UrlTitle = article.UrlTitle;
            }
            else
            {
                model = Mapper.Map<ArticleReadModel>(article);
            }

            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var model = new ArticleCreateModel();
            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(ArticleCreateModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            List<string> tags = SplitTags(model.Tags);

            string title = this.articleRepository.InsertArticle(model.Title, model.Text, tags, this.User.Identity.Name);
            return RedirectToAction("Read", new { id = title });
        }

        private static List<string> SplitTags(string input)
        {
            var tags = new List<string>();

            if (string.IsNullOrEmpty(input))
            {
                return tags;
            }

            char[] separators = { ',', ';' };
            string[] values = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var tag in values)
            {
                string canonical = tag.Trim();  // Could do more: lowercase, remove accents, replace spaces with dashes, etc.
                if (!tags.Contains(canonical))
                {
                    tags.Add(canonical);
                }
            }

            return tags;
        }

        [HttpGet]
        public ActionResult Edit(string id)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return HttpNotFound();
            }

            var model = Mapper.Map<ArticleEditModel>(article);
            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(ArticleEditModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            // TODO: if title has changed, check if title slug has potentially changed, and alert the user.

            this.articleRepository.UpdateArticle(model.ArticleId, model.Title, model.UrlTitle, model.Text, model.Comment, this.User.Identity.Name);
            return RedirectToAction("Read", new { id = model.UrlTitle });
        }

        [HttpGet]
        public ActionResult History(string id)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return HttpNotFound();
            }

            var model = Mapper.Map<ArticleHistoryModel>(article);
            model.HistoryRecords = from a in this.articleRepository.Articles
                                   join h in this.articleRepository.ArticleHistory on a.ArticleId equals h.ArticleId
                                   where a.UrlTitle == id
                                   orderby h.Revision descending
                                   select new ArticleHistoryRecord()
                                   { 
                                       Revision = h.Revision, 
                                       UpdatedBy = h.UpdatedBy, 
                                       UpdatedDtm = h.UpdatedDtm, 
                                       Comment = h.Comment
                                   };

            return View(model);
        }

        [HttpGet]
        public ActionResult Compare(string id, int compareFrom, int compareTo)
        {
            Debug.Assert(compareFrom < compareTo, "compareFrom < compareTo");
            if (compareFrom >= compareTo)
            {
                throw new ArgumentException("compareFrom must be less than compareTo revision", "compareFrom");
            }

            var article = this.articleRepository.Articles.Where(a => a.UrlTitle == id).Select(a => new { a.ArticleId, a.Revision }).FirstOrDefault();
            if (article == null)
            {
                return HttpNotFound();
            }

            string textFrom = null;
            string textTo = null;

            return HttpNotFound();
        }

        [HttpGet]
        public ActionResult GetUniqueTags()
        {
            var tags = this.articleRepository.ArticleTags.Select(t => t.Tag).Distinct().OrderBy(t => t);
            return Json(tags.Select(t => new { label = t, value = t }), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Search(string query)
        {
            var model = new ArticleSearchModel()
            {
                Articles = this.articleRepository.Search(query, this.User.Identity.Name),
                Query = query,
                Tags = this.articleRepository.ArticleTags.Where(t => t.Tag.Contains(query)).Select(t => t.Tag).Distinct()
            };

            return View(model);
        }
    }
}

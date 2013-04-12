namespace Otter.Controllers
{
    using System.Linq;
    using System.Web.Mvc;
    using AutoMapper;
    using Otter.Models;
    using Otter.Repository;
using Otter.Infrastructure;

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
                model.Html = articleRevision.Text == null ? this.htmlConverter.Convert(articleRevision.Text) : this.articleRepository.GetRevisionHtml(articleRevision.ArticleId, articleRevision.Revision);
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

            string title = this.articleRepository.InsertArticle(model.Title, model.Text, this.User.Identity.Name);
            return RedirectToAction("Read", new { id = title });
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
    }
}

namespace Otter.Mvc.Controllers
{
    using System.Linq;
    using System.Web.Mvc;
    using AutoMapper;
    using Otter.Mvc.Models;
    using Otter.Mvc.Repository;

    public class ArticleController : Controller
    {
        private readonly IArticleRepository articleRepository;

        public ArticleController(IArticleRepository articleRepository)
        {
            this.articleRepository = articleRepository;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult Read(string id)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return HttpNotFound();
            }

            var model = Mapper.Map<ArticleReadModel>(article);

            // TODO: sanitize

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

            string title = this.articleRepository.InsertArticle(model.Title, model.Text);
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

            this.articleRepository.UpdateArticle(model.ArticleId, model.Title, model.UrlTitle, model.Text);
            return RedirectToAction("Read", new { id = model.UrlTitle });
        }
    }
}

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
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlFriendlyTitle == id);
            if (article == null)
            {
                return HttpNotFound();
            }

            var model = Mapper.Map<ArticleReadModel>(article);

            var markdown = new MarkdownSharp.Markdown();
            model.Content = markdown.Transform(model.Text);

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
        public ActionResult Create(ArticleCreateModel model)
        {
            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            string title = this.articleRepository.InsertArticle(model.Title, model.Text);
            return RedirectToAction("Read", new { id = title });
        }
    }
}

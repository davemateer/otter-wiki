namespace Otter.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.DirectoryServices.AccountManagement;
    using System.Globalization;
    using System.Linq;
    using System.Web.Mvc;
    using AutoMapper;
    using DiffMatchPatch;
    using Otter.Domain;
    using Otter.Infrastructure;
    using Otter.Models;
    using Otter.Repository;

    public class ArticleController : Controller
    {
        private readonly IArticleRepository articleRepository;
        private readonly ITextToHtmlConverter htmlConverter;
        private readonly ISecurityRepository securityRepository;

        public ArticleController(IArticleRepository articleRepository, ITextToHtmlConverter htmlConverter, ISecurityRepository securityRepository)
        {
            this.articleRepository = articleRepository;
            this.htmlConverter = htmlConverter;
            this.securityRepository = securityRepository;
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ListTags()
        {
            var articles = this.articleRepository.GetTagSummary(this.User.Identity);
            var model = articles.Select(s => new ArticleListTagsRecord() { Tag = s.Item1, Count = s.Item2 }).OrderBy(t => t.Tag);
            return View(model);
        }

        [HttpGet]
        public ActionResult Read(string id, int? revision)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return HttpNotFound();
            }

            if (!this.articleRepository.CanView(this.User, article.ArticleId))
            {
                return new HttpUnauthorizedResult();
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

            this.SetUpdatedDisplayName(model);
            model.Tags = this.articleRepository.ArticleTags.Where(t => t.ArticleId == article.ArticleId).OrderBy(t => t.Tag).Select(t => t.Tag);

            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var model = new ArticleEditModel()
            {
                Security = new PermissionModel(),
                IsNewArticle = true,
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Create(ArticleEditModel model)
        {
            List<ArticleSecurity> security = PopulateSecurityRecords(model.Security, this.ModelState, this.securityRepository.StandardizeUserId(this.User.Identity.Name), this.securityRepository);
            List<string> tags = SplitTags(model.Tags);

            if (!this.ModelState.IsValid)
            {
                return this.View("Edit", model);
            }

            string title = this.articleRepository.InsertArticle(model.Title, model.Text, tags, security, this.securityRepository.StandardizeUserId(this.User.Identity.Name));
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
                if (!string.IsNullOrEmpty(canonical) && !tags.Contains(canonical))
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

            if (!this.articleRepository.CanModify(this.User, article.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            var model = Mapper.Map<ArticleEditModel>(article);
            model.Tags = string.Join(", ", this.articleRepository.ArticleTags.Where(t => t.ArticleId == article.ArticleId).OrderBy(t => t.Tag).Select(t => t.Tag));

            model.Security = new PermissionModel();
            this.articleRepository.HydratePermissionModel(model.Security, article.ArticleId, this.securityRepository.StandardizeUserId(this.User.Identity.Name));

            return View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Edit(ArticleEditModel model)
        {
            if (!this.articleRepository.CanModify(this.User, model.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            List<string> tags = SplitTags(model.Tags);
            List<ArticleSecurity> security = PopulateSecurityRecords(model.Security, this.ModelState, this.securityRepository.StandardizeUserId(this.User.Identity.Name), this.securityRepository);

            if (!this.ModelState.IsValid)
            {
                return this.View(model);
            }

            // TODO: if title has changed, check if title slug has potentially changed, and alert the user.

            this.articleRepository.UpdateArticle(model.ArticleId, model.Title, model.UrlTitle, model.Text, model.Comment, tags, security, this.securityRepository.StandardizeUserId(this.User.Identity.Name));
            return RedirectToAction("Read", new { id = model.UrlTitle });
        }

        private static List<ArticleSecurity> PopulateSecurityRecords(PermissionModel model, ModelStateDictionary modelState, string userId, ISecurityRepository securityRepository)
        {
            // Validate and populate security records
            var security = new List<ArticleSecurity>();

            if (model.ModifyOption == PermissionOption.Everyone)
            {
                security.Add(new ArticleSecurity()
                {
                    Permission = ArticleSecurity.PermissionModify,
                    Scope = ArticleSecurity.ScopeEveryone
                });

                // No need to look any further; this is all inclusive.
                return security;
            }

            switch (model.ModifyOption)
            {
                case PermissionOption.JustMe:
                    security.Add(new ArticleSecurity()
                    {
                        EntityId = userId,
                        Permission = ArticleSecurity.PermissionModify,
                        Scope = ArticleSecurity.ScopeIndividual
                    });

                    break;

                case PermissionOption.Specified:
                    var entities = model.ModifyAccounts.Split(new char[] { ';' }).Select(s => s.Trim());
                    foreach (var entity in entities)
                    {
                        if (string.IsNullOrEmpty(entity))
                        {
                            continue;
                        }

                        try
                        {
                            var found = securityRepository.Find(entity, SecurityEntityTypes.Any);
                            if (found == null)
                            {
                                modelState.AddModelError("Security", "TODO - not found");
                            }
                            else if (!security.Any(s => s.EntityId == found.EntityId && s.Scope == (found.EntityType == SecurityEntityTypes.Group ? ArticleSecurity.ScopeGroup : ArticleSecurity.ScopeIndividual)))
                            {
                                security.Add(new ArticleSecurity()
                                {
                                    EntityId = found.EntityId,
                                    Permission = ArticleSecurity.PermissionModify,
                                    Scope = found.EntityType == SecurityEntityTypes.Group ? ArticleSecurity.ScopeGroup : ArticleSecurity.ScopeIndividual
                                });
                            }
                        }
                        catch (MultipleMatchesException)
                        {
                            modelState.AddModelError("Security", "TODO - duplicate");
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException(string.Format("{0} is not supported.", model.ModifyOption));
            }

            // Now that we know who can modify, add the users who can view.
            switch (model.ViewOption)
            {
                case PermissionOption.Everyone:
                    security.Add(new ArticleSecurity()
                    {
                        Permission = ArticleSecurity.PermissionView,
                        Scope = ArticleSecurity.ScopeEveryone
                    });

                    break;

                case PermissionOption.JustMe:
                    if (!security.Exists(s => s.Scope == ArticleSecurity.ScopeIndividual && s.EntityId == userId))
                    {
                        security.Add(new ArticleSecurity()
                        {
                            EntityId = userId,
                            Permission = ArticleSecurity.PermissionView,
                            Scope = ArticleSecurity.ScopeIndividual
                        });
                    }

                    break;

                case PermissionOption.Specified:
                    var entities = model.ViewAccounts.Split(new char[] { ';' }).Select(s => s.Trim());
                    foreach (var entity in entities)
                    {
                        if (string.IsNullOrEmpty(entity))
                        {
                            continue;
                        }

                        try
                        {
                            var found = securityRepository.Find(entity, SecurityEntityTypes.Any);
                            if (found == null)
                            {
                                modelState.AddModelError("Security", "TODO - not found");
                            }
                            else if (!security.Any(s => s.EntityId == found.EntityId && s.Scope == (found.EntityType == SecurityEntityTypes.Group ? ArticleSecurity.ScopeGroup : ArticleSecurity.ScopeIndividual)))
                            {
                                security.Add(new ArticleSecurity()
                                {
                                    EntityId = found.EntityId,
                                    Permission = ArticleSecurity.PermissionView,
                                    Scope = found.EntityType == SecurityEntityTypes.Group ? ArticleSecurity.ScopeGroup : ArticleSecurity.ScopeIndividual
                                });
                            }
                        }
                        catch (MultipleMatchesException)
                        {
                            modelState.AddModelError("Security", "TODO - duplicate");
                        }
                    }
                    break;

                default:
                    throw new NotSupportedException(string.Format("{0} is not supported.", model.ViewOption));
            }

            return security;
        }

        [HttpGet]
        public ActionResult History(string id)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return HttpNotFound();
            }

            if (!this.articleRepository.CanView(this.User, article.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            var model = Mapper.Map<ArticleHistoryModel>(article);
            this.SetUpdatedDisplayName(model);

            model.HistoryRecords = (from a in this.articleRepository.Articles
                                   join h in this.articleRepository.ArticleHistory on a.ArticleId equals h.ArticleId
                                   where a.UrlTitle == id
                                   orderby h.Revision descending
                                   select new ArticleHistoryRecord()
                                   {
                                       Revision = h.Revision,
                                       UpdatedBy = h.UpdatedBy,
                                       UpdatedDtm = h.UpdatedDtm,
                                       Comment = h.Comment
                                   }).ToList();

            foreach (ArticleHistoryRecord record in model.HistoryRecords)
            {
                this.SetUpdatedDisplayName(record);
            }

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

            ArticleBase article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return HttpNotFound();
            }

            if (!this.articleRepository.CanView(this.User, article.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            ArticleBase articleFrom = this.articleRepository.ArticleHistory.FirstOrDefault(ah => ah.ArticleId == article.ArticleId && ah.Revision == compareFrom);
            if (articleFrom == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Revision {0} is invalid for this article", compareFrom), "compareFrom");
            }

            ArticleBase articleTo = compareTo == article.Revision ? article : this.articleRepository.ArticleHistory.FirstOrDefault(ah => ah.ArticleId == article.ArticleId && ah.Revision == compareTo);
            if (articleTo == null)
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "Revision {0} is invalid for this article", compareTo), "compareTo");
            }

            var model = new ArticleCompareModel
            {
                ArticleId = article.ArticleId,
                CompareFrom = Mapper.Map<ArticleCompareRecord>(articleFrom),
                CompareTo = Mapper.Map<ArticleCompareRecord>(articleTo),
                Title = article.Title,
                UrlTitle = id
            };

            this.SetUpdatedDisplayName(model.CompareFrom);
            this.SetUpdatedDisplayName(model.CompareTo);

            string textFrom = string.IsNullOrEmpty(articleFrom.Text) ? this.articleRepository.GetRevisionText(article.ArticleId, compareFrom) : articleFrom.Text;
            string textTo = string.IsNullOrEmpty(articleTo.Text) ? this.articleRepository.GetRevisionText(article.ArticleId, compareTo) : articleTo.Text;
            diff_match_patch diff = new diff_match_patch();
            List<Diff> diffs = diff.diff_main(textFrom, textTo);
            diff.diff_cleanupSemantic(diffs);
            model.Diff = diff.diff_prettyHtml(diffs);

            return this.View(model);
        }

        [HttpGet]
        public ActionResult GetUniqueTags(string query)
        {
            var tags = this.articleRepository.ArticleTags.Where(t => t.Tag.StartsWith(query)).Select(t => t.Tag).Distinct().OrderBy(t => t);
            return Json(tags.ToList(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Search(string query)
        {
            var model = new ArticleSearchModel
            {
                Query = query
            };

            if (string.IsNullOrEmpty(query))
            {
                model.Articles = new ArticleSearchResult[0];
                model.Tags = new string[0];
            }
            else
            {
                model.Articles = this.articleRepository.SearchByQuery(query, this.User.Identity);
                model.Tags = this.articleRepository.ArticleTags.Where(t => t.Tag.Contains(query)).Select(t => t.Tag).Distinct();
            }

            // TODO: remove items where the user does not have read permissions.

            return View(model);
        }

        [HttpGet]
        public ActionResult Tagged(string id)
        {
            var results = this.articleRepository.SearchByTag(id, this.User.Identity).OrderBy(a => a.Title);

            var model = new ArticleSearchModel()
            {
                Articles = results,
                IsTagSearch = true,
                Query = id,
                Tags = new string[0]
            };

            return View("Search", model);
        }

        [HttpGet]
        public ActionResult Untagged()
        {
            var results = this.articleRepository.SearchByTag(null, this.User.Identity).OrderBy(a => a.Title);

            var model = new ArticleSearchModel()
            {
                Articles = results,
                IsTagSearch = true,
                Query = string.Empty,
                Tags = new string[0]
            };

            return View("Search", model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Preview(string id, string text)
        {
            return Json(new
            {
                id = id,
                html = this.htmlConverter.Convert(text)
            });
        }

        private void SetUpdatedDisplayName(IUpdatedArticle article)
        {
            if (!string.IsNullOrEmpty(article.UpdatedBy))
            {
                var entity = this.securityRepository.Find(article.UpdatedBy, SecurityEntityTypes.User);
                if (entity != null)
                {
                    article.UpdatedByDisplayName = entity.Name;
                }
            }
        }
    }
}


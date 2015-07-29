//-----------------------------------------------------------------------
// <copyright file="ArticleController.cs" company="Dave Mateer">
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
namespace Otter.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.DirectoryServices.AccountManagement;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Mime;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Mvc;
    using AutoMapper;
    using DiffMatchPatch;
    using HtmlAgilityPack;
    using Otter.Domain;
    using Otter.Models;
    using Otter.Repository;

    public class ArticleController : Controller
    {
        private static readonly string ArticleImageVirtualDirectory = ConfigurationManager.AppSettings["otter:ArticleImageVirtualDirectory"];
        private static readonly int MaxAttachmentUploadBytes = int.Parse(ConfigurationManager.AppSettings["otter:MaxAttachmentUploadBytes"]);
        private static readonly int MaxImageUploadBytes = int.Parse(ConfigurationManager.AppSettings["otter:MaxImageUploadBytes"]);
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
                return this.HttpNotFound();
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
            string html = diff.diff_prettyHtml(diffs);

            // Attempt to preserve leading spaces. CSS "white-space: pre-wrap" does not work because
            // it breaks at both the <br> and the &para;
            html = Regex.Replace(html, @"(?<=<br>)(?<spaces> +)", m => string.Concat(Enumerable.Repeat("&nbsp;", m.Groups["spaces"].Length)));

            model.Diff = html;

            return this.View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var model = new ArticleEditModel()
            {
                Security = new PermissionModel(),
                IsNewArticle = true,
            };

            return this.View("Edit", model);
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
            return this.RedirectToAction("Read", new { id = title });
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DeleteAttachment(int id)
        {
            ArticleAttachment attachment = this.articleRepository.ArticleAttachments.SingleOrDefault(i => i.ArticleAttachmentId == id);
            if (attachment == null)
            {
                return this.HttpNotFound();
            }

            Article article = this.articleRepository.Articles.SingleOrDefault(a => a.ArticleId == attachment.ArticleId);
            if (article == null)
            {
                return this.HttpNotFound();
            }

            if (!this.articleRepository.CanModify(this.User, article.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            this.articleRepository.DeleteArticleAttachment(id);
            string attachmentPath = GetAttachmentPath(article.UrlTitle, attachment.Filename);
            System.IO.File.Delete(attachmentPath);

            IEnumerable<ArticleAttachmentRecordModel> attachments = this.BuildArticleAttachmentRecordModels(article);
            this.ViewData["edit"] = true;
            return this.PartialView("Attachments", attachments);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DeleteImage(int articleImageId)
        {
            ArticleImage image = this.articleRepository.ArticleImages.SingleOrDefault(i => i.ArticleImageId == articleImageId);
            if (image == null)
            {
                return this.HttpNotFound();
            }

            Article article = this.articleRepository.Articles.SingleOrDefault(a => a.ArticleId == image.ArticleId);
            if (article == null)
            {
                return this.HttpNotFound();
            }

            if (!this.articleRepository.CanModify(this.User, article.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            this.articleRepository.DeleteArticleImage(articleImageId);
            string imagePath = GetImagePath(article.UrlTitle, image.Filename);
            System.IO.File.Delete(imagePath);
            return this.RedirectToAction("Images", new { id = article.UrlTitle });
        }

        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Head)]
        public ActionResult DownloadAttachment(int id)
        {
            ArticleAttachment attachment = this.articleRepository.ArticleAttachments.SingleOrDefault(a => a.ArticleAttachmentId == id);
            if (attachment == null)
            {
                return this.HttpNotFound();
            }

            if (!this.articleRepository.CanView(this.User, attachment.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            ContentDisposition contentDisposition = new ContentDisposition
            {
                FileName = attachment.Filename,
                Inline = false
            };

            this.Response.AppendHeader("Content-Disposition", contentDisposition.ToString());

            string urlTitle = this.articleRepository.Articles.Single(a => a.ArticleId == attachment.ArticleId).UrlTitle;
            string attachmentPath = GetAttachmentPath(urlTitle, attachment.Filename);
            return this.File(attachmentPath, "application/octet-stream");
        }

        [HttpGet]
        public ActionResult Edit(string id)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return this.HttpNotFound();
            }

            if (!this.articleRepository.CanModify(this.User, article.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            var model = Mapper.Map<ArticleEditModel>(article);
            model.Attachments = this.BuildArticleAttachmentRecordModels(article);
            model.ImageCount = this.articleRepository.ArticleImages.Count(i => i.ArticleId == article.ArticleId);
            model.Tags = string.Join(", ", this.articleRepository.ArticleTags.Where(t => t.ArticleId == article.ArticleId).OrderBy(t => t.Tag).Select(t => t.Tag));

            model.Security = new PermissionModel();
            this.articleRepository.HydratePermissionModel(model.Security, article.ArticleId, this.securityRepository.StandardizeUserId(this.User.Identity.Name));

            return this.View(model);
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

            // TODO: if title has changed, check if title slug has potentially changed, and alert
            //       the user.
            this.articleRepository.UpdateArticle(model.ArticleId, model.Title, model.UrlTitle, model.Text, model.Comment, tags, security, this.securityRepository.StandardizeUserId(this.User.Identity.Name));
            return this.RedirectToAction("Read", new { id = model.UrlTitle });
        }

        [HttpGet]
        public ActionResult GetUniqueTags(string query)
        {
            var tags = this.articleRepository.ArticleTags.Where(t => t.Tag.StartsWith(query)).Select(t => t.Tag).Distinct().OrderBy(t => t);
            return this.Json(tags.ToList(), JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult History(string id)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return this.HttpNotFound();
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
                                        UpdatedWhen = h.UpdatedWhen,
                                        Comment = h.Comment
                                    }).ToList();

            foreach (ArticleHistoryRecord record in model.HistoryRecords)
            {
                this.SetUpdatedDisplayName(record);
            }

            return this.View(model);
        }

        [HttpGet]
        public ActionResult Images(string id)
        {
            var article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return this.HttpNotFound();
            }

            if (!this.articleRepository.CanModify(this.User, article.ArticleId))
            {
                return new HttpUnauthorizedResult();
            }

            ArticleImageModel model = this.BuildArticleImageModel(article);
            return this.View(model);
        }

        [HttpGet]
        public ActionResult Index()
        {
            return this.View();
        }

        [HttpGet]
        public ActionResult ListTags()
        {
            var articles = this.articleRepository.GetTagSummary(this.User.Identity);
            IEnumerable<ArticleListTagsRecord> model = articles.Select(s => new ArticleListTagsRecord() { Tag = s.Item1, Count = s.Item2 }).OrderBy(t => t.Tag);
            return this.View(model);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult Preview(string id, string text, string articleId)
        {
            return this.Json(new
            {
                id = id,
                html = ResolveApplicationPaths(this.htmlConverter.Convert(text, articleId))
            });
        }

        [HttpGet]
        public ActionResult Read(string id, int? revision)
        {
            Article article = this.articleRepository.Articles.FirstOrDefault(a => a.UrlTitle == id);
            if (article == null)
            {
                return this.HttpNotFound();
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
                    return this.HttpNotFound();
                }

                model = Mapper.Map<ArticleReadModel>(articleRevision);
                model.Html = articleRevision.Text == null ? this.articleRepository.GetRevisionHtml(articleRevision.ArticleId, articleRevision.Revision) : this.htmlConverter.Convert(articleRevision.Text, article.UrlTitle);
                model.UrlTitle = article.UrlTitle;
            }
            else
            {
                model = Mapper.Map<ArticleReadModel>(article);
            }

            model.Html = ResolveApplicationPaths(model.Html);

            // Resolve user ids to names.
            this.SetUpdatedDisplayName(model);

            if (!string.IsNullOrEmpty(model.CreatedBy))
            {
                var entity = this.securityRepository.Find(model.CreatedBy, SecurityEntityTypes.User);
                if (entity != null)
                {
                    model.CreatedByDisplayName = entity.Name;
                }
            }

            model.Tags = this.articleRepository.ArticleTags.Where(t => t.ArticleId == article.ArticleId).OrderBy(t => t.Tag).Select(t => t.Tag);
            model.Attachments = this.BuildArticleAttachmentRecordModels(article);
            return this.View(model);
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
            return this.View(model);
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

            return this.View("Search", model);
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

            return this.View("Search", model);
        }

        [HttpPost]
        public ActionResult UploadAttachment(ArticleUploadFileModel model)
        {
            Article article = this.articleRepository.Articles.SingleOrDefault(a => a.ArticleId == model.ArticleId);
            if (article == null)
            {
                return this.HttpNotFound();
            }

            if (this.ModelState.IsValid)
            {
                if (!this.articleRepository.CanModify(this.User, model.ArticleId.Value))
                {
                    return new HttpUnauthorizedResult();
                }

                // Ensure that the uploaded file does not exceed the maximum size.
                if (!IsValidAttachment(model.UploadFile))
                {
                    this.ModelState.AddModelError("UploadFile", string.Format("The uploaded file must be a valid image file under {0} in size.", HtmlHelperExtensions.FileSize(MaxAttachmentUploadBytes)));
                }
            }

            if (this.ModelState.IsValid)
            {
                string filename = this.CreateUniqueArticleAttachmentFilename(model.ArticleId.Value, model.UploadFile.FileName);
                string saveAsPath = GetAttachmentPath(article.UrlTitle, filename);
                Directory.CreateDirectory(Path.GetDirectoryName(saveAsPath));
                model.UploadFile.SaveAs(saveAsPath);
                this.articleRepository.InsertArticleAttachment(model.ArticleId.Value, filename, model.UploadTitle);
            }

            IEnumerable<ArticleAttachmentRecordModel> attachments = this.BuildArticleAttachmentRecordModels(article);
            this.ViewData["edit"] = true;
            return this.PartialView("Attachments", attachments);
        }

        [HttpPost]
        public ActionResult UploadImage(ArticleUploadFileModel model)
        {
            Article article = this.articleRepository.Articles.SingleOrDefault(a => a.ArticleId == model.ArticleId);
            if (article == null)
            {
                return this.HttpNotFound();
            }

            if (this.ModelState.IsValid)
            {
                if (!this.articleRepository.CanModify(this.User, model.ArticleId.Value))
                {
                    return new HttpUnauthorizedResult();
                }

                // Ensure that the uploaded file is an image, and that it does not exceed the
                // maximum size.
                if (!IsValidImage(model.UploadFile))
                {
                    this.ModelState.AddModelError("UploadFile", string.Format("The uploaded file must be a valid image file under {0} in size.", HtmlHelperExtensions.FileSize(MaxImageUploadBytes)));
                }
            }

            if (!this.ModelState.IsValid)
            {
                ArticleImageModel imageModel = this.BuildArticleImageModel(article);
                return this.View("Images", imageModel);
            }

            string filename = this.CreateUniqueArticleImageFilename(model.ArticleId.Value, model.UploadFile.FileName);
            string saveAsPath = GetImagePath(article.UrlTitle, filename);
            Directory.CreateDirectory(Path.GetDirectoryName(saveAsPath));
            model.UploadFile.SaveAs(saveAsPath);
            this.articleRepository.InsertArticleImage(model.ArticleId.Value, filename, model.UploadTitle);

            return this.RedirectToAction("Images", new { id = article.UrlTitle });
        }

        private static string GetAttachmentPath(string urlTitle, string filename)
        {
            return Path.Combine(ConfigurationManager.AppSettings["otter:ArticleAttachmentFolder"], urlTitle, filename);
        }

        private static string GetImagePath(string urlTitle, string filename)
        {
            return Path.Combine(ConfigurationManager.AppSettings["otter:ArticleImageFolder"], urlTitle, filename);
        }

        private static bool IsValidAttachment(HttpPostedFileBase postedFile)
        {
            return postedFile.ContentLength <= MaxAttachmentUploadBytes;
        }

        private static bool IsValidImage(HttpPostedFileBase postedFile)
        {
            if (postedFile.ContentLength > MaxImageUploadBytes)
            {
                return false;
            }

            try
            {
                ImageFormat[] allowedFormats = new[] { ImageFormat.Jpeg, ImageFormat.Png, ImageFormat.Gif, ImageFormat.Bmp };
                using (Image img = Image.FromStream(postedFile.InputStream))
                {
                    return allowedFormats.Contains(img.RawFormat);
                }
            }
            catch (ArgumentException)
            {
                return false;
            }
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

        private static string ResolveApplicationPaths(string html)
        {
            HtmlDocument doc = new HtmlDocument()
            {
                OptionFixNestedTags = true,
                OptionAutoCloseOnEnd = true
            };

            doc.LoadHtml(html);

            HtmlNodeCollection imgNodes = doc.DocumentNode.SelectNodes("//img");
            if (imgNodes != null)
            {
                foreach (HtmlNode img in doc.DocumentNode.SelectNodes("//img"))
                {
                    HtmlAttribute src = img.Attributes["src"];
                    if (src.Value.StartsWith("%ARTICLE_IMAGES%/", StringComparison.OrdinalIgnoreCase))
                    {
                        img.Attributes.Add("class", "img-responsive");
                        src.Value = src.Value.Replace("%ARTICLE_IMAGES%/", VirtualPathUtility.ToAbsolute(string.Format("~/{0}/", ArticleImageVirtualDirectory)));
                    }
                }
            }

            return doc.DocumentNode.OuterHtml;
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

        private IEnumerable<ArticleAttachmentRecordModel> BuildArticleAttachmentRecordModels(Article article)
        {
            IEnumerable<ArticleAttachment> attachments = this.articleRepository.ArticleAttachments.Where(a => a.ArticleId == article.ArticleId);
            List<ArticleAttachmentRecordModel> attachmentRecords = new List<ArticleAttachmentRecordModel>(attachments.Count());
            foreach (ArticleAttachment attachment in attachments)
            {
                ArticleAttachmentRecordModel record = new ArticleAttachmentRecordModel
                {
                    ArticleId = attachment.ArticleId,
                    ArticleAttachmentId = attachment.ArticleAttachmentId,
                    Filename = attachment.Filename,
                    IsValid = false,
                    Title = attachment.Title
                };

                string attachmentPath = GetAttachmentPath(article.UrlTitle, attachment.Filename);
                FileInfo attachmentInfo = new FileInfo(attachmentPath);
                if (attachmentInfo.Exists)
                {
                    record.Bytes = attachmentInfo.Length;
                    record.CreationTime = attachmentInfo.CreationTime;
                    record.IsValid = true;
                }

                attachmentRecords.Add(record);
            }

            return attachmentRecords.OrderBy(a => a.IsValid).ThenBy(a => a.CreationTime);
        }

        private ArticleImageModel BuildArticleImageModel(Article article)
        {
            IEnumerable<ArticleImage> images = this.articleRepository.ArticleImages.Where(i => i.ArticleId == article.ArticleId);
            List<ArticleImageRecordModel> imageRecords = new List<ArticleImageRecordModel>(images.Count());
            foreach (ArticleImage image in images)
            {
                ArticleImageRecordModel record = new ArticleImageRecordModel
                {
                    ArticleId = image.ArticleId,
                    ArticleImageId = image.ArticleImageId,
                    Filename = image.Filename,
                    IsValid = false,
                    Title = image.Title
                };

                string imagePath = GetImagePath(article.UrlTitle, image.Filename);
                FileInfo imageInfo = new FileInfo(imagePath);
                if (imageInfo.Exists)
                {
                    record.Bytes = imageInfo.Length;
                    record.CreationTime = imageInfo.CreationTime;
                    try
                    {
                        using (Image img = Image.FromFile(imagePath))
                        {
                            record.Dimensions = img.Size;
                        }
                    }
                    catch (ArgumentException)
                    {
                    }

                    record.IsValid = true;
                }

                imageRecords.Add(record);
            }

            ArticleImageModel model = new ArticleImageModel
            {
                ArticleId = article.ArticleId,
                Images = imageRecords,
                Title = article.Title,
                UrlTitle = article.UrlTitle
            };

            return model;
        }

        private string CreateUniqueArticleAttachmentFilename(int articleId, string baseFilename)
        {
            string filename = Path.GetFileName(baseFilename);
            ArticleAttachment[] existingAttachments = this.articleRepository.ArticleAttachments.Where(a => a.ArticleId == articleId).ToArray();
            if (existingAttachments.Any(i => i.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase)))
            {
                int suffix = 1;
                do
                {
                    filename = string.Format("{0}-{1}{2}", Path.GetFileNameWithoutExtension(baseFilename), suffix++, Path.GetExtension(baseFilename));
                }
                while (existingAttachments.Any(i => i.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase)));
            }

            return filename;
        }

        private string CreateUniqueArticleImageFilename(int articleId, string baseFilename)
        {
            string filename = Path.GetFileName(baseFilename);
            ArticleImage[] existingImages = this.articleRepository.ArticleImages.Where(i => i.ArticleId == articleId).ToArray();
            if (existingImages.Any(i => i.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase)))
            {
                int suffix = 1;
                do
                {
                    filename = string.Format("{0}-{1}{2}", Path.GetFileNameWithoutExtension(baseFilename), suffix++, Path.GetExtension(baseFilename));
                }
                while (existingImages.Any(i => i.Filename.Equals(filename, StringComparison.OrdinalIgnoreCase)));
            }

            return filename;
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
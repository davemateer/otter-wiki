namespace Otter.Controllers
{
    using System.Collections.Generic;
    using System.Web.Mvc;
    using Otter.Domain;
    using Otter.Repository;

    public sealed class SecurityController : Controller
    {
        private readonly ISecurityRepository securityRepository;

        public SecurityController(ISecurityRepository securityRepository)
        {
            this.securityRepository = securityRepository;
        }

        public ActionResult SearchUsersAndGroups(string query)
        {
            var searchResults = this.securityRepository.Search(query);
            List<string> displayResults = new List<string>();

            foreach (var result in searchResults)
            {
                if (result.EntityType == SecurityEntityTypes.Group)
                {
                    displayResults.Add(string.Format("{0} (Group)", result.EntityId));
                }
                else
                {
                    displayResults.Add(string.Format("{0} [{1}]", result.Name, result.EntityId));
                }
            }

            displayResults.Sort();
            return Json(displayResults, JsonRequestBehavior.AllowGet);
        }
    }
}

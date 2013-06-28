namespace Otter.Controllers
{
    using System.Linq;
    using System.Web.Mvc;
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
            var displayResults = searchResults.Select(r => r.ToString()).OrderBy(s => s);
            return Json(displayResults, JsonRequestBehavior.AllowGet);
        }
    }
}

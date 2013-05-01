namespace Otter.Controllers
{
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Web.Mvc;

    public sealed class SecurityController : Controller
    {
        public ActionResult SearchUsersAndGroups(string query)
        {
            // TODO: sanitize query

            var matches = new List<string>();

            using (var searcher = new DirectorySearcher())
            {
                searcher.Filter = string.Format("(&(objectCategory=person)(objectClass=user)(|(givenName={0}*)(sn={0}*)(sAMAccountName={0}*)))", query);
                
                using (SearchResultCollection results = searcher.FindAll())
                {
                    foreach (SearchResult found in results)
                    {
                        matches.Add(found.Properties["displayName"][0].ToString());
                    }
                }
            }

            matches.Sort();

            return Json(matches, JsonRequestBehavior.AllowGet);
        }
    }
}

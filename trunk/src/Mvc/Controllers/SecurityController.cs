namespace Otter.Controllers
{
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Text.RegularExpressions;
    using System.Web.Mvc;

    public sealed class SecurityController : Controller
    {
        public ActionResult SearchUsersAndGroups(string query)
        {
            // Safe, but restrictive white-list method of preventing LDAP injection. If other characters are ever needed (accents, etc.),
            // this may need extended. This removes leading and trailing spaces and anything other than the 26 letters of the English alphabet, 
            // 0-9 numbers, and embedded spaces.
            query = Regex.Replace(query.Trim(), "[^A-Za-z0-9 ]", string.Empty);

            var matches = new List<string>();

            using (var searcher = new DirectorySearcher())
            {
                searcher.Filter = string.Format("(&(objectCategory=person)(objectClass=user)(|(givenName={0}*)(sn={0}*)(displayName={0}*)(sAMAccountName={0}*)))", query);
                
                using (SearchResultCollection results = searcher.FindAll())
                {
                    foreach (SearchResult found in results)
                    {
                        matches.Add(string.Format("{0} [{1}]", found.Properties["displayName"][0], found.Properties["sAMAccountName"][0]));
                    }
                }

                searcher.Filter = string.Format("(&(objectCategory=group)(cn={0}*))", query);

                using (SearchResultCollection results = searcher.FindAll())
                {
                    foreach (SearchResult found in results)
                    {
                        matches.Add(string.Format("{0} (Group)", found.Properties["cn"][0]));
                    }
                }
            }

            matches.Sort();

            return Json(matches, JsonRequestBehavior.AllowGet);
        }
    }
}

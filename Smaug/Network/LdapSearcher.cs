using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Smaug
{
    class LdapSearcher
    {
        public static HashSet<string> GetDomainComputers()
        {
            var hostnames = new HashSet<string>();

            try
            {
                var search_base = string.Format("LDAP://{0}", GetDefaultNamingContext());

                using (var dir_entry = new DirectoryEntry(search_base))
                {
                    using (var dir_search = new DirectorySearcher(dir_entry, "(&(objectClass=computer)(dNSHostName=*))"))
                    {
                        // dir_search.PageSize = ?
                        // dir_search.PropertiesToLoad = ?

                        using (var dir_results = dir_search.FindAll())
                        {
                            foreach (SearchResult dir_result in dir_results)
                            {
                                hostnames.Add(dir_result.Properties["dNSHostName"][0].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Printer.Error("Error: {0}", e.Message);
            }

            return hostnames;
        }

        private static string GetDefaultNamingContext()
        {
            using (var root_dse = new DirectoryEntry("LDAP://RootDSE"))
            {
                return (string)root_dse.Properties["defaultNamingContext"].Value;
            }
        }
    }
}

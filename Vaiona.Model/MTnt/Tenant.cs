using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vaiona.Model.MTnt
{
    public class Tenant
    {
        public string Id { get; set; }
        public string ShortName { get; set; }
        public string Description { get; set; }
        public string LogoPath { get; set; }
        public string ThemeName { get; set; }
        /// <summary>
        /// One or more matching rules to resolve the tenant.
        /// The matching rules are regular expressions to be examined against the incoming http request.
        /// </summary>
        public List<string> MatchingRules { get; set; }
    }
}

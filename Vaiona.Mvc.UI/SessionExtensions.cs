using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.SessionState;
using System.Globalization;

namespace Vaiona.Mvc.UI
{
    public static class SessionExtensions
    {
        public static void SetDomainUser(this HttpSessionState session)
        {
            throw new NotImplementedException();
        }

        public static void ApplyCulture(this HttpSessionState session, string cultureId)
        {
            session.ApplyCulture(new CultureInfo(cultureId, true));
        }

        public static void ApplyCulture(this HttpSessionState session, CultureInfo culture)
        {
            if (culture.Name.Equals("fa-IR", StringComparison.InvariantCultureIgnoreCase))
            {
                // perform calendar and etc settings
            }
            // ...
        }
    }
}

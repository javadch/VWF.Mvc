using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.SessionState;
using System.Globalization;
using System.Threading;
using Vaiona.Web.Helpers;

namespace Vaiona.Web.Extensions
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
            GlobalizationHelper.SetSessionCulture(culture);
        }

        public static CultureInfo GetCurrentCulture(this HttpSessionState session)
        {
            return (GlobalizationHelper.GetCurrentCulture());
        }
    
    }
}

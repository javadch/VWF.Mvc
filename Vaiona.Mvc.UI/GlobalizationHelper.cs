using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;
using System.Web;

namespace Vaiona.Mvc.UI
{
    public class GlobalizationHelper
    {
        public static void SetSessionCulture(CultureInfo culture)
        {
            if (culture.Name.Equals("fa-IR", StringComparison.InvariantCultureIgnoreCase))
            {
                // perform calendar and etc settings
            }
            HttpContext.Current.Session["SessionCulture"] = culture;
        }

        public static CultureInfo GetCurrentCulture()
        {
            return (Thread.CurrentThread.CurrentCulture);
        }

        public static CultureInfo GetSessionCulture()
        {
            return (HttpContext.Current.Session["SessionCulture"] as CultureInfo);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Vaiona.Utils.Cfg;

namespace Vaiona.Web.Mvc.Models
{
    public class PresentationModel: Dictionary<string, object>
    {
        private string viewTitle = string.Empty;
        internal string ViewTitle // needs a proper design. internal for the time being...
        { 
            get
            { 
                return viewTitle;
            } 
            set 
            {
                viewTitle = value;
            } 
        }

        public static string GetViewTitle(string viewTitle)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(viewTitle));

            string appInfo = AppConfiguration.ApplicationInfo;
            if (!string.IsNullOrWhiteSpace(appInfo))
            {
                return string.Format("{0} - {1}", appInfo, viewTitle);
            }
            return viewTitle;
        }
    }
}

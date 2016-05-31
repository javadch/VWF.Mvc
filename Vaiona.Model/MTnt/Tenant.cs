﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vaiona.Utils.Cfg;

namespace Vaiona.Model.MTnt
{
    public enum TenantStatus
    {
        Active,
        Inactive
    }
    public class Tenant
    {
        public ITenantPathProvider PathProvider { get; set; } // to be injected automatically by the IoC
        public string Id { get; set; }

        public bool UseFallback { get; set; }
        public Tenant Fallback { get; set; }
        /// <summary>
        /// the abbreviation of the tenant 
        /// </summary>
        public string ShortName { get; set; }
        
        /// <summary>
        /// A one short line slogan style, used in brower tabs and information section along with the short name
        /// </summary>
        public string Title { get; set; }
        
        /// <summary>
        /// A one paragraph descrotion of the tenant
        /// </summary>
        public string Description { get; set; }

        public string InfoBar { get { return string.Format("{0}: {1}", ShortName, Title); } }

        /// <summary>
        /// The logo should be located in /<tenant>/images/<name> of /<default>/images/<name.ext> if not provided bt the manifest
        /// </summary>
        public string Logo { get; set; }

        /// <summary>
        /// The icon should be located in /<tenant>/images/<name.ext> of /<default>/images/<name.ext> if not provided bt the manifest
        /// </summary>
        public string FavIcon { get; set; }

        /// <summary>
        /// by default it is the application's default theme. If provided, first the tenant ptovided theme will be used, if the actual theme does not exist in the tenant's folder, the one in the application is used.
        /// </summary>
        public string Theme { get; set; }

        /// <summary>
        /// by default it is the application's default layout, otherwise the layout is located in the the chosen theme folder
        /// The layout must have its accompanying layout.xml
        /// </summary>
        public string Layout { get; set; }
        
        /// <summary>
        /// The default's tenant landing page, if nor provided.
        /// It can not be an external url.
        /// </summary>
        public string LandingPage { get; set; }

        /// <summary>
        /// Extended menus are links to external URLs and presented differently (comparing to the built-in menus) to the tenant's users
        /// </summary>
        public XElement ExtendedMenus { get; set; }
        public TenantStatus Status { get; set; }
        
        /// <summary>
        /// One or more matching rules to resolve the tenant.
        /// The matching rules are regular expressions to be examined against the incoming http request.
        /// </summary>
        public List<string> MatchingRules { get; set; }
        public bool IsDefault { get; set; }
        public string PolicyFileName { get; set; }
        public string ContactUsFileName { get; set; }
        public string ImprintFileName { get; set; }
        public string ContactEmail { get; set; }
        public string SupportEmail { get; set; }

        public string LogoPath //effective path to logo
        {
            get
            {
                if (this.UseFallback == true && this.Fallback != null)
                    return PathProvider.GetImagePath(this.Id, this.Logo, Fallback.Id);
                else
                    return PathProvider.GetImagePath(this.Id, this.Logo, this.Id); // The second this.Id argument is passed to allow the Client TenantPathProviders to have a chance of getting triggered.
            }
        } 
        public string FavIconPath //effective path to FavIcon
        {
            get
            {
                if (this.UseFallback == true && this.Fallback != null)
                    return PathProvider.GetImagePath(this.Id, this.FavIcon, Fallback.Id);
                else
                    return PathProvider.GetImagePath(this.Id, this.FavIcon, this.Id);
            }
        }

        public string ThemePath //effective path to Theme
        {
            get
            {
                if (this.UseFallback == true && this.Fallback != null)
                    return PathProvider.GetThemePath(this.Id, this.Theme, Fallback.Id);
                else
                    return PathProvider.GetThemePath(this.Id, this.Theme, this.Id);
            }
        }

        public string PolicyFileNamePath //effective path to policy content file
        {
            get
            {
                if (this.UseFallback == true && this.Fallback != null)
                    return PathProvider.GetContentFilePath(this.Id, this.PolicyFileName, Fallback.Id);
                else
                    return PathProvider.GetContentFilePath(this.Id, this.PolicyFileName, this.Id);
            }
        }

        public string ContactUsFileNamePath //effective path to contact us content file
        {
            get
            {
                if (this.UseFallback == true && this.Fallback != null)
                    return PathProvider.GetContentFilePath(this.Id, this.ContactUsFileName, Fallback.Id);
                else
                    return PathProvider.GetContentFilePath(this.Id, this.ContactUsFileName, this.Id);
            }
        }

        public string ImprintFileNamePath //effective path to imprint content file
        {
            get
            {
                if (this.UseFallback == true && this.Fallback != null)
                    return PathProvider.GetContentFilePath(this.Id, this.ImprintFileName, Fallback.Id);
                else
                    return PathProvider.GetContentFilePath(this.Id, this.ImprintFileName, this.Id);
            }
        }

        public List<string> AllowedFileExtensions { get; set; }

        public int MaximumUploadSize { get; set; }
    }
}
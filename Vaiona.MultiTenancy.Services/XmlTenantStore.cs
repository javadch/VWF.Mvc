using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vaiona.Model.MTnt;
using Vaiona.Utils.Cfg;

namespace Vaiona.MultiTenancy.Services
{
    /// <summary>
    /// Performs the persisting operations on an XML store
    /// 
    /// </summary>
    /// <remarks>It would be better that this class is part of the domain specific applications that utilize Vaiona, 
    /// So that it is injected into the resolver/registrar at runtime using the IoC.</remarks>
    public class XmlTenantStore
    {
        internal string ManifestFilePath { get
            {
                return Path.Combine(AppConfiguration.WorkspaceTenantsRoot, "tenants.manifest.xml");
            }
        }

        public XmlTenantStore(ITenantPathProvider pathProvider)
        {
            this.pathProvider = pathProvider;
        }
        private ITenantPathProvider pathProvider;
        // consider using function call caching
        private static List<Tenant> tenants = new List<Tenant>(); // for caching purposes. Needs more work!

        public List<Tenant> Tenants { get { return tenants; } }

        public List<Tenant> Load()
        {
            tenants.Clear();
            var manifest = LoadManifest();
            Tenant defaultTenant = manifest.Where(p => p.IsDefault == true).SingleOrDefault();
            if (!string.IsNullOrWhiteSpace(defaultTenant.Id))
            {
                tenants.Add(LoadTenant(defaultTenant, null)); // the default tenant must be loaded first
                manifest.Where(p=> defaultTenant.Id != p.Id).ToList() // other manifests, except the default one. It is already loaded
                    .ForEach(p => tenants.Add(LoadTenant(p, defaultTenant)));
            }
            else
                manifest.ForEach(p => tenants.Add(LoadTenant(p, null)));
            tenants.ForEach(p => p.PathProvider = pathProvider);
            return tenants;
        }
     
        /// <summary>
        /// Use the tenants manifest file to list the tenant manifest information
        /// </summary>
        /// <returns></returns>
        public List<Tenant> LoadManifest()
        {
            List<Tenant> tenants = new List<Tenant>();
            // get the list from the registry entry
            // load all from the tenent XML entires
            XElement manifest = XElement.Load(ManifestFilePath);
            IEnumerable<XElement> xTenants = manifest.Elements("Tenant");
            foreach (var xTenant in xTenants)
            {
                Tenant tenant = new Tenant();
                try
                {
                    tenant.Id = xTenant.Attribute("id").Value;
                }catch { throw new Exception("One of the tenants in the root manifest file has a missing identifier!"); }
                try
                {
                    tenant.IsDefault = bool.Parse(xTenant.Attribute("default").Value);
                }
                catch { tenant.IsDefault = false; }
                try
                {
                    tenant.Status = "active".Equals(xTenant.Attribute("status").Value, StringComparison.InvariantCultureIgnoreCase) 
                        ? TenantStatus.Active : TenantStatus.Inactive; 
                }catch { tenant.Status = TenantStatus.Inactive; }
                tenants.Add(tenant);
            }

            return tenants;
        }

        /// <summary>
        /// Load the XML stored tenant, transform it to a tenant object and return.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Tenant LoadTenant(Tenant tenant, Tenant defaultTenant)
        {
            // each tenant has its own folder. Inside the folder there is a manifest file containg the textual information of the tenent.
            // Its named manifest.xml
            //empty manifest attributes will be taken from the fallback tenant, if requested by the manifest
            //bu default bexis is the fallback tenant. 
            // When loading check if logo, gavicon, privacy policty, etc are provided be the tenant. If not load them from the default
            if (tenant.Status != TenantStatus.Active)
                return tenant;
            string tenantManifestFile = GetTenantManifestFile(tenant.Id);
            XElement manifest = null;
            try
            {
                manifest = XElement.Load(tenantManifestFile);
            }
            catch
            {
                throw new Exception(string.Format("The tenant's manifest file for tenant '{0}' was not found.", tenant.Id));
            }

            try
            {
                string xId = manifest.Attribute("id").Value;
                if (!tenant.Id.Equals(xId, StringComparison.InvariantCultureIgnoreCase))
                    throw new Exception(string.Format("The tenant's manifest file does not match the provided id '{0}'.", tenant.Id));
            }
            catch
            {
                throw new Exception(string.Format("The tenant's manifest file does not match the provided id '{0}'.", tenant.Id));
            }

            try
            {
                tenant.UseFallback = bool.Parse(manifest.Attribute("useFallback").Value);
                if(tenant.UseFallback)
                    tenant.Fallback = defaultTenant;
            }
            catch
            {
                tenant.UseFallback = false;
                tenant.Fallback = null;
            }
            
            try
            {
                tenant.ShortName = manifest.Element("ShortName").Value;
            } catch
            {
                if(tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.ShortName = defaultTenant.ShortName;
            }

            try
            {
                tenant.Title = manifest.Element("Title").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.Title = defaultTenant.Title;
            }

            try
            {
                tenant.Description = manifest.Element("Description").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.Description = defaultTenant.Description;
            }

            try
            {
                tenant.Logo = manifest.Element("Logo").Value;                
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                {
                    tenant.Logo = defaultTenant.Logo;                    
                }
            }

            try
            {
                tenant.FavIcon = manifest.Element("FavIcon").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                {
                    tenant.FavIcon = defaultTenant.FavIcon;
                }
            }

            try
            {
                tenant.Theme = manifest.Element("Theme").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                {
                    tenant.Theme = defaultTenant.Theme;
                }
            }

            try
            {
                tenant.Layout = manifest.Element("Layout").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.Layout = defaultTenant.Layout;                
            }
            // take another chance to set the layout
            if (string.IsNullOrWhiteSpace(tenant.Layout))
            {
                tenant.Layout = AppConfiguration.ActiveLayoutName;
            }

            try
            {
                tenant.LandingPage = manifest.Element("LandingPage").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.LandingPage = defaultTenant.LandingPage;
            }

            try
            {
                tenant.PolicyFileName = manifest.Element("PolicyFileName").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                {
                    tenant.PolicyFileName = defaultTenant.PolicyFileName;
                }
            }

            try
            {
                tenant.ContactUsFileName = manifest.Element("ContactUsFileName").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                {
                    tenant.ContactUsFileName = defaultTenant.ContactUsFileName;
                }
            }

            try
            {
                tenant.ImprintFileName = manifest.Element("ImprintFileName").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                {
                    tenant.ImprintFileName = defaultTenant.ImprintFileName;
                }
            }

            try
            {
                tenant.ContactEmail = manifest.Element("ContactEmail").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.ContactEmail = defaultTenant.ContactEmail;
            }

            try
            {
                tenant.SupportEmail = manifest.Element("SupportEmail").Value;
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.SupportEmail = defaultTenant.SupportEmail;
            }

            try
            {
                tenant.MatchingRules = new List<string>();
                foreach (var matchingRule in manifest.Element("MatchingRules").Elements("MatchingRule"))
                {
                    if (!string.IsNullOrWhiteSpace(matchingRule.Value) && !tenant.MatchingRules.Contains(matchingRule.Value))
                        tenant.MatchingRules.Add(matchingRule.Value);
                }
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    //tenant.MatchingRules.AddRange(tenant.Fallback.MatchingRules); // may cause confusion in tenant resolution
                    tenant.MatchingRules.Clear();
            }

            try
            {
                tenant.AllowedFileExtensions = manifest.Element("AllowedFileExtensions").Value
                    .Split(',').ToList()
                    .Select(p=> p.Trim()).ToList(); // removes possible leading or tailing white spaces
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.AllowedFileExtensions = defaultTenant.AllowedFileExtensions;
            }

            try
            {
                tenant.MaximumUploadSize = int.Parse(manifest.Element("MaximumUploadSize").Value);
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.MaximumUploadSize = defaultTenant.MaximumUploadSize;
            }

            try
            {
                tenant.ExtendedMenus = manifest.Element("ExtendedMenus");//.Elements("ExtendedMenu");
            }
            catch
            {
                if (tenant.UseFallback == true && tenant.Fallback != null)
                    tenant.ExtendedMenus = defaultTenant.ExtendedMenus;
            }

            return tenant;
        }

        /// <summary>
        /// Transforms the tenant to its XML representation and stores it
        /// </summary>
        /// <param name="tenant"></param>
        public void Create(Tenant tenant)
        {
            // do the job
            Load(); //reload
        }

        /// <summary>
        /// Updates the registry entries. Used in activate, inactivate scenarios
        /// </summary>
        /// <param name="tenant"></param>
        public void UpdateStatus(Tenant tenant)
        {
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Id))
                throw new Exception(string.Format("No tenant information is provided."));
            // do the job
            try
            {
                XElement manifest = XElement.Load(ManifestFilePath);
                XElement xTenant;
                xTenant = manifest.Elements("Tenant")
                    .Where(p => tenant.Id.Equals(p.Attribute("id").Value, StringComparison.InvariantCultureIgnoreCase))
                    .Single();
                xTenant.SetAttributeValue("status", tenant.Status == TenantStatus.Active ? "active" : "inactive");
                manifest.Save(ManifestFilePath);
                Load(); //reload
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Tenant {0} was not found.", tenant.Id));
            }
        }

        public void MakeDefault(Tenant tenant)
        {
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Id))
                throw new Exception(string.Format("No tenant information is provided."));
            // do the job
            try
            {
                XElement manifest = XElement.Load(ManifestFilePath);
                // set all to default == false
                foreach (var xTenantToBeUnset in manifest.Elements("Tenant"))
                {
                    xTenantToBeUnset.SetAttributeValue("default", false);
                }
                XElement xTenant =manifest.Elements("Tenant")
                   .Where(p => tenant.Id.Equals(p.Attribute("id").Value, StringComparison.InvariantCultureIgnoreCase))
                   .Single();
                // set the chosen one to default
                xTenant.SetAttributeValue("default", true);
                manifest.Save(ManifestFilePath);
                Load();
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Tenant {0} was not found.", tenant.Id));
            }

        }

        public void Remove(Tenant tenant)
        {
            if (tenant == null || string.IsNullOrWhiteSpace(tenant.Id))
                throw new Exception(string.Format("No tenant information is provided."));
            // do the job
            try
            {
                XElement manifest = XElement.Load(ManifestFilePath);
                XElement xTenant;
                xTenant = manifest.Elements("Tenant")
                    .Where(p => tenant.Id.Equals(p.Attribute("id").Value, StringComparison.InvariantCultureIgnoreCase))
                    .Single();
                // remove the entry from the manifest file
                xTenant.Remove();
                // delete the tenant package folder from the file system
                // if done, save the changes to the manifest
                manifest.Save(ManifestFilePath);
                Load(); //reload
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Tenant {0} was not found.", tenant.Id));
            }
        }

        internal string GetTenantManifestFile(string id)
        {
            return Path.Combine(AppConfiguration.WorkspaceTenantsRoot, id, "manifest.xml");
        }

    }
}

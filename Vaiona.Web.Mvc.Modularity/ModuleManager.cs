using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Routing;
using System.Xml.Linq;
using Vaiona.Utils.Cfg;

namespace Vaiona.Web.Mvc.Modularity
{
    public class ModuleManager
    {
        private const string catalogFileName = "Modules.Catalog.xml";
        private static FileSystemWatcher watcher = new FileSystemWatcher();

        private static List<ModuleInfo> moduleInfos { get; set; }
        /// <summary>
        /// The readonly list of the plugins.
        /// </summary>
        public static List<ModuleInfo> ModuleInfos { get { return moduleInfos; } }

        public static XElement ExportTree = new XElement("Export", ".");

        private static XElement catalog;
        public static XElement Catalog // it may need caching, etc
        {
            get
            {
                return catalog;

            }
        }

        static ModuleManager()
        {
            moduleInfos = new List<ModuleInfo>();
            loadCatalog();
            watcher.Path = AppConfiguration.WorkspaceModulesRoot;
            /* Watch for changes in LastAccess and LastWrite times, and
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            // Only watch the manifest file.
            watcher.Filter = catalogFileName;

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(onCatalogChanged);
            watcher.Created += new FileSystemEventHandler(onCatalogChanged);
            watcher.Deleted += new FileSystemEventHandler(onCatalogChanged);
            watcher.Renamed += new RenamedEventHandler(onCatalogChanged);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

        }

        private static void loadCatalog()
        {
            string filePath = Path.Combine(AppConfiguration.WorkspaceModulesRoot, catalogFileName);
            Vaiona.Utils.IO.FileHelper.WaitForFile(filePath);
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read))
            {
                catalog = XElement.Load(stream);
            }
        }
        private static void onCatalogChanged(object source, FileSystemEventArgs e)
        {
            loadCatalog();
            // refresh the status of all the modules.
            //Refresh();
        }

        /// <summary>
        /// Refreshes the status of all the modules.
        /// It diables all the modules first and then enables only those that are marked active in the catalog file.
        /// </summary>
        public static void Refresh()
        {
            var moduleIds = from element in catalog.Elements("Module")
                            select element.Attribute("id").Value;
            foreach (var moduleId in moduleIds)
            {
                Disable(moduleId, false);
                if (IsActive(moduleId))
                    Enable(moduleId, false);
            }
        }

        public static void Register(string moduleId)
        {
            // unzip the foler into the areas folder
            // check the manifest
            // add entry to the catalog, if catalog does not exist: create it
            // set the status to inactive.
            // load the assembly
            // install the routes, etc.
        }
        public static void Upgrade(string moduleId)
        {

        }

        public static void UnRegister(string moduleId)
        {

        }

        public static bool IsActive(XElement moduleElement)
        {
            bool isActive = moduleElement.Attribute("status").Value.Equals("Active", StringComparison.InvariantCultureIgnoreCase) ? true : false;
            return isActive;
        }
        public static bool IsActive(string moduleId)
        {
            var isActive = from entry in catalog.Elements("Module")
                           where (entry.Attribute("id").Value.Equals(moduleId, StringComparison.InvariantCultureIgnoreCase))
                           select (entry.Attribute("status").Value.Equals("Active", StringComparison.InvariantCultureIgnoreCase) ? true : false);
            //XElement mElement = getCatalogEntry(moduleId);
            // if (mElement == null)
            //     return false;
            // return IsActive(mElement);
            return (isActive != null ? isActive.FirstOrDefault() : false);
        }
        public static void Disable(string moduleId, bool updateCatalog = true)
        {
            var module = get(moduleId);
            if (module != null && module.Plugin != null)
            {
                if (updateCatalog == true)
                {
                    // update the catalog
                    var cachedCatalog = catalog;
                    var catalogEntry = cachedCatalog.Elements("Module")
                                              .Where(x => x.Attribute("id").Value.Equals(moduleId, StringComparison.InvariantCultureIgnoreCase))
                                              .FirstOrDefault();
                    if (catalogEntry == null)
                        return;
                    catalogEntry.SetAttributeValue("status", "inactive");
                    watcher.EnableRaisingEvents = false;
                    cachedCatalog.Save(Path.Combine(AppConfiguration.WorkspaceModulesRoot, catalogFileName));
                    watcher.EnableRaisingEvents = true;
                }
                // remove the default route, and possible the others starting with <moduleId>
                // is not needed. The ModularMvcRouteHandler takes care of it
                //foreach (var item in module.Plugin.ModuleRoutes)
                //{
                //    RouteTable.Routes.Remove(RouteTable.Routes[item.Key]);
                //}
            }
        }

        internal static void Add(ModuleInfo moduleMetadata)
        {
            if (moduleInfos.Count(p => p.Id.Equals(moduleMetadata.Id, StringComparison.InvariantCultureIgnoreCase)) > 0)
                return;
            moduleInfos.Add(moduleMetadata);
            // add the current module's exports to the ModuleManager export ExportTree.
            // Only the UI exports for now.
        }

        public static void Enable(string moduleId, bool updateCatalog = true)
        {
            var module = get(moduleId);
            if (module != null && module.Plugin != null)
            {
                if (updateCatalog == true)
                {
                    // update the catalog
                    var cachedCatalog = catalog;
                    var catalogEntry = cachedCatalog.Elements("Module")
                                              .Where(x => x.Attribute("id").Value.Equals(moduleId, StringComparison.InvariantCultureIgnoreCase))
                                              .FirstOrDefault();
                    if (catalogEntry == null)
                        return;
                    catalogEntry.SetAttributeValue("status", "active");
                    watcher.EnableRaisingEvents = false;
                    cachedCatalog.Save(Path.Combine(AppConfiguration.WorkspaceModulesRoot, catalogFileName));
                    watcher.EnableRaisingEvents = true;
                }
                // install the default route, and possible the others starting with <moduleId>
                // route are not removed. See ModularMvcRouteHandler
                //foreach (var item in module.Plugin.ModuleRoutes)
                //{
                //    if (RouteTable.Routes[item.Key] == null)
                //    {
                //        RouteTable.Routes.Add(item.Key, item.Value);
                //    }
                //}
            }
        }

        public static ModuleInfo GetModuleInfo(string moduleId)
        {
            return get(moduleId);
        }
        private static ModuleInfo get(string moduleId)
        {
            return moduleInfos.Where(m => m.Manifest.Name.Equals(moduleId, StringComparison.InvariantCultureIgnoreCase)).SingleOrDefault();
        }

        private static XElement getCatalogEntry(string moduleId)
        {
            var entry = catalog.Elements("Module")
                .Where(x => x.Attribute("id").Value.Equals(moduleId, StringComparison.InvariantCultureIgnoreCase))
                .FirstOrDefault();
            return entry;
        }
    }

}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Routing;
using System.Xml.Linq;
using Vaiona.Utils.Cfg;
using Vaiona.Utils.IO;
using Ionic.Zip;

namespace Vaiona.Web.Mvc.Modularity
{
    public class ModuleManager
    {
        private static Dictionary<string, Assembly> moduleAssemblyCache = new Dictionary<string, Assembly>();
        public static string DeploymentRoot = HostingEnvironment.MapPath("~/Areas");
        private const string catalogFileName = "Modules.Catalog.xml";
        private static FileSystemWatcher watcher = new FileSystemWatcher();

        private static List<ModuleInfo> moduleInfos { get; set; }
        /// <summary>
        /// The readonly list of the plugins.
        /// </summary>
        public static List<ModuleInfo> ModuleInfos { get { return moduleInfos; } }

        private static XElement exportTree = null;
        public static XElement ExportTree { get { return exportTree; } }

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

        private static string shellManifestPath;
        public static void RegisterShell(string shellManifestPath)
        {
            ModuleManager.shellManifestPath = shellManifestPath;
        }

        /// <summary>
        /// Creates the Shell's export items, Placeholders for other menu items that will be coming from the modules, and 
        /// provides the tag space for the menu locations.
        /// It then build the export structure of the modules.
        /// The modules must register their exports only to the tag space and the hierarchy exposed by the shell.
        /// </summary>
        public static void BuildExportTree()
        {
            // build the shell's export tree
            exportTree = new XElement("Exports", new XAttribute("id", "."), new XAttribute("tag", "all"));
            XElement manifest = XElement.Load(shellManifestPath);
            buildExportParts(manifest.Element("Exports").Elements("Export"), "");

            // integrate active modules' exports into the overall tree.
            var moduleIds = from element in catalog.Elements("Module")
                            orderby int.Parse(element.Attribute("order").Value) // apply ordering, it may affect the order of menu items in the UI
                            select element.Attribute("id").Value;
                            
            foreach (var moduleId in moduleIds)
            {
                if (IsActive(moduleId))
                {
                    // take the export entries, add an attribute: moduleId = value to them, and
                    // add them to proper node based on their tag and extends attribute
                    // set the area to the module Id.
                    var moduleInfo = get(moduleId);
                    buildExportParts(moduleInfo.Manifest.ManifestDoc.Element("Exports").Elements("Export"), moduleId);
                }
            }
            resolveNameConflicts(exportTree);
        }

        private static void resolveNameConflicts(XElement node)
        {
            if(node.Elements() != null)
            {
                // check for identical titles, and prefix them with the moduleID if found 
                foreach (var current in node.Elements("Export"))
                {
                    resolveNameConflicts(current); // resolve the children of the current node

                    // find title conflicts between the current node and its sibling after itself.
                    bool currentMustChange = false;
                    foreach (var sibling in current.ElementsAfterSelf("Export"))
                    {
                        if (current.Attribute("title").Value.Equals(sibling.Attribute("title").Value, StringComparison.InvariantCultureIgnoreCase))
                        {
                            currentMustChange = true;
                            // set the sibling export node's title
                            if (!string.IsNullOrWhiteSpace(sibling.Attribute("area").Value)) // Shell items are not prefixed
                            {
                                string newTitle = string.Format("{0}: {1}", sibling.Attribute("area").Value.Capitalize(), sibling.Attribute("title").Value);
                                sibling.SetAttributeValue("title", newTitle);
                            }
                        }
                    }
                    // when done comaring to all the siblings, apply the needed changes to the current node.
                    if(currentMustChange)
                    {
                        // set the current node's title
                        if (!string.IsNullOrWhiteSpace(current.Attribute("area").Value)) // Shell items are not prefixed
                        {
                            string newTitle = string.Format("{0}: {1}", current.Attribute("area").Value.Capitalize(), current.Attribute("title").Value);
                            current.SetAttributeValue("title", newTitle);
                        }
                    }
                }
            }
        }

        private static void buildExportParts(IEnumerable<XElement> exportItems, string areaName)
        {
            foreach (var export in exportItems)
            {
                string extends = export.Attribute("extends").Value;
                string tag = export.Attribute("tag").Value;
                if (string.IsNullOrWhiteSpace(tag))
                    throw new Exception("The tag attributes of menu items can not be null or empty.");

                // break down the extends attribute and traverse the exportTree over the same tag branch
                // at ant level either find the relevant node or create it
                // if the found node matches the end of the path, it is a node previously created during traversal, so it must be updated
                List<string> extendPath = string.IsNullOrWhiteSpace(extends) ? new List<string>() : extends.Split('/').ToList();

                XElement current = exportTree;
                int pathCounter = 0;
                foreach (var pathElement in extendPath)
                {
                    if (pathElement.Equals("."))
                    {
                        current = exportTree;
                    }
                    else
                    {
                        XElement foundNode = current.Elements("Export")
                            .Where(p =>
                                p.Attribute("id").Value.Equals(pathElement, StringComparison.InvariantCultureIgnoreCase)
                                && p.Attribute("tag").Value.Equals(tag, StringComparison.InvariantCultureIgnoreCase)
                            ).FirstOrDefault();
                        if (foundNode == null) // create an element
                        {
                            foundNode = new XElement("Export",
                                                new XAttribute("title", pathElement.Capitalize()),
                                                new XAttribute("id", pathElement),
                                                new XAttribute("order", current.Elements() != null ? current.Elements().Count() + 1 : 1),
                                                new XAttribute("extends", string.Join("/", extendPath.Skip(0).Take(pathCounter))),
                                                new XAttribute("tag", tag));
                            current.Add(foundNode);
                        }
                        current = foundNode;
                    }
                    pathCounter++;
                }
                // enforce the area name, so that no module can register an export point on behalf of other modules
                export.SetAttributeValue("area", areaName);
                // assign an order to the export point if not provided
                if (export.Attribute("order") == null || string.IsNullOrWhiteSpace(export.Attribute("order").Value))
                    export.SetAttributeValue("order", current.Elements() != null ? current.Elements().Count() + 1 : 1);
                // locate the child node that the export point should be added before it.
                XElement insertBeforeThis = null;
                if(current.Elements() != null)
                {
                    int exportOrder = int.Parse(export.Attribute("order").Value);
                    foreach (var child in current.Elements("Export"))
                    {
                        int childOrder = int.Parse(child.Attribute("order").Value);
                        if(exportOrder < childOrder) // only <, because this export node is coming later, so in case of =, the existing one has precedence.
                        {
                            insertBeforeThis = child;
                            break;
                        }
                    }
                }
                // add the export to the right place
                if (insertBeforeThis != null)
                {
                    insertBeforeThis.AddBeforeSelf(export);
                }
                else // add it to the end
                {
                    current.Add(export);
                }

            }
        }

        private static void loadCatalog()
        {
            string filePath = Path.Combine(AppConfiguration.WorkspaceModulesRoot, catalogFileName);
            FileHelper.WaitForFile(filePath);
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
            BuildExportTree();
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
            BuildExportTree();
        }

        /// <summary>
        /// Receives a module in a zipped file and installs or upgrades it on the system
        /// </summary>
        /// <param name="bundle">The module's required resources in zipped form. The bundle contains the binaries, views, static and dynamic resources, and the manifest.</param>
        public static void Install(HttpPostedFileBase bundle, bool installForProduction = true)
        {
            // unzip the foler into the areas folder
            // check the manifest
            // add entry to the catalog, if catalog does not exist: create it
            // set the status to pending.
            // load the assembly? may need restart
            // install the routes, etc.
                        
            string moduleName = "";
            string moduleVersion = "";
            string installationPath = Path.Combine(AppConfiguration.WorkspaceModulesRoot, "installing");
            string path = "";

            /// 1: Acquire the bundle, check if the same bundle is installing? if yes, return an error
            if (bundle == null || bundle.ContentLength <= 0)
                throw new System.Exception(string.Format("The submited file is not valid. Operation aborted."));

            var fileName = Path.GetFileName(bundle.FileName);
            moduleName = fileName.Substring(0, fileName.IndexOf("."));
            moduleVersion = fileName.Substring(fileName.IndexOf(".") + 1, (fileName.LastIndexOf(".") - fileName.IndexOf(".") - 1));

            path = installationPath;
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = Path.Combine(path, fileName);
            if (System.IO.File.Exists(path)) // the module is being installed from another session
            {
                throw new Exception(string.Format("Bundle {0} is being installed from another session or a previous installation attempt has failed. Operation aborted.", fileName));
            }

            /// 2: Save the bundle in the "installing" folder for further processing.
            bundle.SaveAs(path);

            /// 3: Validate the bundle
            validateBundle(moduleName, moduleVersion, installationPath); // throws exception on validation issues

            /// 4: Distribute the content of the bundle to proper places
            copyBundleElements(moduleName, moduleVersion, installationPath, installForProduction);

            /// 5: Register the module in the modules catalog
            registerModule(moduleName);

            /// 6: Delete the zipped bundle any any temprorary file.folder created during installation
            cleanUpInstallation(moduleName, moduleVersion, installationPath); // expect empty parameters

        }

        /// <summary>
        /// When a module is registered for the first time, it is in "pending" status.
        /// Later on its first load by the shell, it will be checked for schema export, etc. and transfered to "inactive"
        /// </summary>
        /// <returns></returns>
        public static bool HasPendingInstallation()
        {
            var pendingModules = from m in catalog.Elements("Module")
                                    where m.Attribute("status").Value.Equals("Pending", StringComparison.InvariantCultureIgnoreCase)
                                    select m;
            return (pendingModules.Count() > 0);
        }
        
        /// <summary>
        /// Lists the modules thata re currently in the pending state.
        /// Pending state indicates that the module is in installation process.
        /// </summary>
        /// <returns></returns>
        public static List<string> PendingModules()
        {
            var pendingModules = from m in catalog.Elements("Module")
                                 where m.Attribute("status").Value.Equals("Pending", StringComparison.InvariantCultureIgnoreCase)
                                 select m.Attribute("id").Value;
            return (pendingModules.ToList());
        }

        public static void Upgrade(string moduleId)
        {

        }

        public static void Uninstall(string moduleId)
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
            setStatus(moduleId, "inactive", updateCatalog);
            if (updateCatalog == true)
            {
                BuildExportTree();
            }
        }

        public static void Enable(string moduleId, bool updateCatalog = true)
        {
            setStatus(moduleId, "active", updateCatalog);
            if(updateCatalog == true)
            {
                BuildExportTree();
            }
        }

        public static void SetStatus(string moduleId, string status, bool updateCatalog = true)
        {
            setStatus(moduleId, status, updateCatalog);
            if (updateCatalog == true)
            {
                BuildExportTree();
            }
        }

        private static void setStatus(string moduleId, string status, bool updateCatalog)
        {
            // NOTE: application of updateCatalog needs clarification.
            // The ModularMvcRouteHandler checks the status to route the request or to prevent it.
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
                    catalogEntry.SetAttributeValue("status", status);
                    watcher.EnableRaisingEvents = false;
                    cachedCatalog.Save(Path.Combine(AppConfiguration.WorkspaceModulesRoot, catalogFileName));
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        internal static void Add(ModuleInfo moduleMetadata)
        {
            if (moduleInfos.Count(p => p.Id.Equals(moduleMetadata.Id, StringComparison.InvariantCultureIgnoreCase)) > 0)
                return;
            moduleInfos.Add(moduleMetadata);
            // add the current module's exports to the ModuleManager export ExportTree.
            //buildModuleExportTree(moduleMetadata.Id);
        }

        public static ModuleInfo GetModuleInfo(string moduleId)
        {
            return get(moduleId);
        }

        public static ModuleManifest GetModuleManifest(string moduleId)
        {
            var moduleInfo = get(moduleId);
            if (moduleInfo != null && moduleInfo.Manifest != null)
                return moduleInfo.Manifest;
            
            string manifestPath = Path.Combine(ModuleManager.DeploymentRoot, moduleId, string.Format("{0}.Manifest.xml", moduleId));
            ModuleManifest manifest = new ModuleManifest(manifestPath);
            return manifest;
        }

        public static bool IsModuleLoaded(string moduleId)
        {
            return moduleInfos.Count(m => m.Id.Equals(moduleId, StringComparison.InvariantCultureIgnoreCase) && m.Plugin != null) > 0;
        }

        public static void CacheAssembly(string assemblyName, Assembly assembly)
        {
            if (!moduleAssemblyCache.ContainsKey(assemblyName))
                moduleAssemblyCache.Add(assemblyName, assembly);
        }

        /// <summary>
        /// This method is bound to the AppDomain.CurrentDomain.AssemblyResolve event so that when the appDomain
        /// requests to resolve an assembly, the plugin assemblies are resolved from the already loaded plugin cache managed by the PluginManager class.
        /// </summary>
        /// <param name="sender">not used.</param>
        /// <param name="args">contains the assembly named requested and optioanlly the assemby itself.</param>
        /// <returns>The resolved assembly, the cached plugin assembly</returns>
        /// <remarks>Exceptions are managed nby the callaing code.</remarks>
        public static Assembly ResolveCurrentDomainAssembly(object sender, ResolveEventArgs args)
        {
            if (args.RequestingAssembly != null)
                return args.RequestingAssembly;
            // At this point, the catalog may be checked to see if the requested assembly belongs to a disabled module, the resolution should fail.                
            //ModuleBase module = pluginManager.ModuleInfos.
            //    SingleOrDefault(x => (x.EntryType.Assembly.FullName == args.Name) && (x.Manifest.IsEnabled == true)).Plugin;

            string asmName = new AssemblyName(args.Name).Name;

            var moduleIfo = ModuleInfos
                .Where(x => (x.EntryType.Assembly.FullName.Equals(asmName, StringComparison.InvariantCultureIgnoreCase))
                //&& (x.Manifest.IsEnabled == true) // check the catalog
                )
                .FirstOrDefault();
            if (moduleIfo != null)
            {
                return moduleIfo.EntryType.Assembly;
            }

            // check the module cache
            string asmNameEx = asmName + ".dll";
            if (moduleAssemblyCache.ContainsKey(asmNameEx))
                return moduleAssemblyCache[asmNameEx];

            throw new Exception(string.Format("Unable to load assembly {0}", asmName));
        }

        public static void StartModules()
        {
            moduleInfos.ForEach(module => module.Plugin.Start());
        }

        public static void ShutdownModules()
        {
            moduleInfos.ForEach(module => module.Plugin.Shutdown());
        }

        /// <summary>
        /// Creates or updates a module registration entry in the catalog, sets its status and order. It may reorder the other modules if needed.
        /// </summary>
        /// <param name="moduleId">The module's identifier</param>
        /// <param name="status">The status that the module will hold after registaration</param>
        /// <param name="order">The module's order in catalog. defulat value zero (0) put the modules at the end of catlog entries.</param>
        /// <param name="updateIfExists">Determines weather the entry should be updated if it already exists. Otherwise an exception is thrown.</param>
        private static void registerModule(string moduleId, string status = "pending", int order = 0, bool updateIfExists = true)
        {
            var cachedCatalog = catalog;
            XElement catalogEntry = cachedCatalog.Elements("Module")
                                      .Where(x => x.Attribute("id").Value.Equals(moduleId, StringComparison.InvariantCultureIgnoreCase))
                                      .FirstOrDefault();
            if (catalogEntry != null)
            {
                if (updateIfExists == false) // entry exists, but should not be updated
                {
                    throw new Exception(string.Format("Module '{0}' already exists but is not allowed to be altered.", moduleId));
                }
                else // detach the entry so that the rest of the algorithm functions properly.
                {
                    catalogEntry.Remove();
                }
            }
            // either the module does not exist, or it is detached from the list to be updated
            if (catalogEntry == null) // the module does not exist in the catalog
            {
                catalogEntry = new XElement("Module");
            }

            // set/update the entry's attributes
            catalogEntry.SetAttributeValue("id", moduleId);
            catalogEntry.SetAttributeValue("status", status);
            catalogEntry.SetAttributeValue("order", order > 0 ? order : moduleInfos.Count + 1); // by default it is set to the end.

            // update the order attribute of the other modules
            foreach (var entry in cachedCatalog.Elements("Module").Where(x => int.Parse(x.Attribute("order").Value) >= order && order > 0))
            {
                entry.SetAttributeValue("order", int.Parse(entry.Attribute("order").Value) + 1);
            }

            // add the created/updated entry to cached catalog
            cachedCatalog.Add(catalogEntry);

            //sort catalog on "order" ascending and apply the ordering changes on the casched catalog
            var sortedCatalogItems = cachedCatalog.Elements("Module")
                                                    .OrderBy(p => int.Parse(p.Attribute("order").Value))
                                                    .ToList();
            cachedCatalog.Elements("Module").Remove();
            cachedCatalog.Add(sortedCatalogItems);

            // disable the catalog watcher, apply the changes onto catalog and enable the watcher
            // this method updates the catalog file without causing modules reloaded.
            watcher.EnableRaisingEvents = false;
            cachedCatalog.Save(Path.Combine(AppConfiguration.WorkspaceModulesRoot, catalogFileName));
            watcher.EnableRaisingEvents = true;
        }


        private static void copyBundleElements(string moduleName, string moduleVersion, string installationPath, bool installForProduction = true)
        {
            //throw new NotImplementedException();
            string zipPath = Path.Combine(installationPath, moduleName + "." + moduleVersion + ".zip");
            var zip = ZipFile.Read(zipPath);
            string tempPath = Path.Combine(installationPath, moduleName, moduleVersion);
            try
            {
                if (Directory.Exists(tempPath))
                    Directory.Delete(tempPath);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Could not delete directoty {0}. {1}", tempPath, ex.Message));
            }
            try
            {
                Directory.CreateDirectory(tempPath);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Could not create directoty {0}. {1}", tempPath, ex.Message));
            }
            try
            {
                zip.ExtractAll(tempPath);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("Could not unzip bundle {0} to directory {1}. {2}", moduleName, tempPath, ex.Message));
            }

            // in debug mode, there is no need to copy the files because they are already in the right place and more importantly the bundle will overwite the sources!
           
            if (installForProduction)
            {
                //move the workspace, if exists in the bundle
                string moduleWorkspace = Path.Combine(AppConfiguration.WorkspaceModulesRoot, moduleName);
                try
                {
                    if (Directory.Exists(moduleWorkspace))
                    {
                        // remove the module's workspace and its content. 
                        // It may remove existing module specific settings. So better guard it with a user selected switch (UI)
                        //Directory.Delete(moduleWorkspace, true);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Could not delete directoty {0}. {1}", moduleWorkspace, ex.Message));
                }
                try
                {
                    FileHelper.MoveAndReplace(Path.Combine(tempPath, "Workspace"), moduleWorkspace);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Could not move the bundle's worspace to {0}. {1}", moduleWorkspace, ex.Message));
                }
                // move whatever else that needs to go to a place other than the module's root

                // move whatever is remained to the module's root. This should include bin, Content, Scripts, and Views folder plus the manifest file.
                string moduleDepolymentPath = Path.Combine(ModuleManager.DeploymentRoot, moduleName, moduleVersion); // the version is ONLY for testing purpose. must be removed!
                try
                {
                    if (Directory.Exists(moduleDepolymentPath))
                        Directory.Delete(moduleDepolymentPath, true);
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Could not delete directoty {0}. {1}", moduleDepolymentPath, ex.Message));
                }
                try
                {
                    // remove the bin folder's assemblies that are not listed in the manifest

                    // move the reamining 
                    Directory.Move(tempPath, moduleDepolymentPath); // in case use MoveAndReplace
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("Could not move the bundle's binaries to {0}. {1}", moduleDepolymentPath, ex.Message));
                }
            }
            //end if
        }

        private static void cleanUpInstallation(string moduleName, string moduleVersion, string installationPath)
        {
            // delete the temprary module folder created during installation
            Directory.GetDirectories(installationPath, moduleName).ToList()
                .ForEach(p => Directory.Delete(p, true));
            // delete the uploaded zip bundle
            Directory.GetFiles(installationPath, string.Format("{0}.*.zip", moduleName)).ToList()
                .ForEach(p => File.Delete(p));
        }

        private static void validateBundle(string moduleName, string moduleVersion, string installationPath)
        {
            // check the module version: zip, manifest, UI assembly.
            // check if an equal or a higher version is already installed
            var existingModule = ModuleManager.GetModuleInfo(moduleName);
            if (existingModule != null)
            {
                Version exisitingVersion = new Version(existingModule.Manifest.Version);
                Version incomingVersion = new Version(moduleVersion);
                if (exisitingVersion >= incomingVersion)
                {
                    throw new Exception(string.Format("A higher version of the '{0}' module is already installed.", moduleName));
                }
            }
            // check if the declaraed dependecies are already available. module/version

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

﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Compilation;
using System.Web.Hosting;
using System.Xml.Linq;
using Vaiona.Utils.Cfg;

namespace Vaiona.Web.Mvc.Modularity
{
    /// <summary>
    /// This class is responsible for loading the plugin assemblies before the application is started.
    /// Its Initialize method automatically fires at app_pre_init, because it is registered in the AssemblyInfo.cs 
    /// The method should read the assembly names from the modules catalog, select the active ones and 
    /// load their associated assemblies from the modules folder into the plugins folder.
    /// The plugins folder is resgisted as a probling folder in the web.config, 
    /// so that the AppDomain knows to search there for loading assemblies.
    /// This project must be referenced from the Shell to get activated.
    /// </summary>
    /// <remarks>
    /// The main issue with this method is that it needs the application to be restarted 
    /// if a new module is registered or its status has been changed!! No hot plugability yet!!!!
    /// </remarks>
    public class ModuleInitializer
    {
        /// <summary>
        /// The source folder the conatins the modules' assemblies
        /// </summary>
        /// <remarks>
        /// This folder conatins one folder per module, so that each folder conatins all the resources of the respective module
        /// </remarks>
        private static readonly DirectoryInfo areasFolder;
        static ModuleInitializer()
        {
            // this code is called by aspnet_compiler at compile time!!
            // The if protects it from running during the build
            // See: http://stackoverflow.com/questions/13642691/avoid-aspnet-compiler-running-preapplicationstart-method
            if (HostingEnvironment.InClientBuildManager)
                return;

            // Use AppConfiguration for path selection
            string areasPath = HostingEnvironment.MapPath("~/Areas");
            areasFolder = new DirectoryInfo(areasPath);
            if (areasFolder == null || !areasFolder.Exists)
                throw new DirectoryNotFoundException("Areas");
        }

        public static void Initialize()
        {
            if (HostingEnvironment.InClientBuildManager)
                return;
            // it is possible that the module list and statuses are fetched from a catalog file. 
            // for now each folder in the Areas folder means a potential module
            XElement catalog = XElement.Load(Path.Combine(AppConfiguration.WorkspaceModulesRoot, "Modules.Catalog.xml"));
            var activeModules = from m in catalog.Elements("Module")
                                where m.Attribute("status").Value.Equals("Active", StringComparison.InvariantCultureIgnoreCase)
                                select m;
            foreach (var moduleEntry in activeModules)
            //foreach (var moduleDir in areasFolder.GetDirectories())
            {
                string moduleId = moduleEntry.Attribute("id").Value;
                if (string.IsNullOrWhiteSpace(moduleId))
                    break;
                var moduleDir = areasFolder.GetDirectories(moduleId, SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (moduleDir == null || !moduleDir.Exists)
                    break;

                string manifestFileName = string.Format("{0}.Manifest.xml", moduleDir.Name);
                if (moduleDir.GetFiles(manifestFileName, SearchOption.TopDirectoryOnly).Count() == 1)
                {
                    try
                    {
                        var moduleBinFolder = moduleDir.GetDirectories("bin", SearchOption.TopDirectoryOnly).First();
                        // for each module get the its main assembly, the one tha contains the type inheritted from the ModuleBase
                        string assemblyNamePattern = string.Format("*.{0}.UI.dll", moduleDir.Name);
                        var assemblieNames = moduleBinFolder.GetFiles(assemblyNamePattern, SearchOption.TopDirectoryOnly)
                                                            .Select(x => AssemblyName.GetAssemblyName(x.FullName))
                                                            .ToList();
                        foreach (var asmName in assemblieNames)
                        {
                            var asm = Assembly.Load(asmName);
                            // check for for a class that inherits from the ModuleBase class
                            Type type = asm.GetTypes()
                                           .Where(t => typeof(ModuleBase).IsAssignableFrom(t)).FirstOrDefault();
                            if (type != null)
                            {
                                ModuleInfo moduleMetadata = new ModuleInfo();
                                moduleMetadata.Id = moduleDir.Name;
                                moduleMetadata.EntryType = type;
                                // instance will be created later with the area registration
                                //var plugin = (ModuleBase)Activator.CreateInstance(type);
                                // Check for duplicates
                                if (!ModuleManager.ModuleInfos.Contains(moduleMetadata))
                                {
                                    //Add the plugin (module's entry assembly/type) as a reference to the application
                                    BuildManager.AddReferencedAssembly(asm);
                                    moduleMetadata.Path = moduleDir;
                                    //Add the modules to the PluginManager to manage them later
                                    ModuleManager.Add(moduleMetadata);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("Module {0} does not have the proper structure! No bin folder found.", moduleDir.Name), ex);
                    }
                }
                else
                {
                    throw new Exception(string.Format("Folder: {0} located at: {1} is supposed to be a module, but does not contain a valid manifest file.", moduleDir.Name, moduleDir.FullName));
                }
            }
        }
    }
}

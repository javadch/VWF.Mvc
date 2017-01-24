using System;
using System.Linq;
using System.Reflection;

namespace Vaiona.Web.Mvc.Modularity
{
    public static class ModuleBootstrapper
    {
        static ModuleBootstrapper()
        {
        }

        /// <summary>
        /// register the embedded views of the plugins by registering thier containing assemblies
        /// </summary>
        public static void Initialize()
        {
            foreach (var plugin in ModuleManager.ModuleInfos)
            {
                //BoC.Web.Mvc.PrecompiledViews.ApplicationPartRegistry.Register(plugin.EntryType.Assembly);
            }
        }

        public static void StartModules()
        {
            ModuleManager.ModuleInfos.ForEach(module => module.Plugin.Start());
        }

        public static void ShutdownModules()
        {
            ModuleManager.ModuleInfos.ForEach(module => module.Plugin.Shutdown());
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
            var moduleIfo = ModuleManager.ModuleInfos
                .Where(x => (x.EntryType.Assembly.FullName.Equals(args.Name, StringComparison.InvariantCultureIgnoreCase))
                //&& (x.Manifest.IsEnabled == true) // check the catalog
                )
                .FirstOrDefault();
            if (moduleIfo != null)
            {
                return moduleIfo.EntryType.Assembly;
            }
            throw new Exception(string.Format("Unable to load assembly {0}", args.Name));
        }
    }
}

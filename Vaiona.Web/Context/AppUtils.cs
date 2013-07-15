using System.Web;
using Vaiona.Persistence.Api;
using Vaiona.IoC;
using Vaiona.Util.Cfg;
using System.Linq;
using System.Collections.Generic;
using System.Web.Mvc;
using System;
using Vaiona.Web.Mvc;
using System.Reflection;

namespace Vaiona.Web.Context
{
    public static class AppUtils
    {
        public static Assembly ApplicationAssembly = null;

        public static void Init(HttpApplication app, Assembly applicationAssembly)
        {
            string appRoot = app.Server.MapPath("~/").TrimEnd(@"\".ToCharArray()) + @"\";
            ApplicationAssembly = applicationAssembly;
            IoCFactory.StartContainer(string.Format("{0}{1}", appRoot, "IoC.config"), "DefaultContainer");
            IPersistenceManager pManager = PersistenceFactory.GetPersistenceManager(); // just to prepare data access environment

            if (!string.IsNullOrWhiteSpace(AppConfiguration.DatabaseMappingFile))
            {
                // change this folder path to something relative to the web root
                //Its possible to have more than one mapping folder, but the first one should point to the place of configuration file too.
                System.Collections.Generic.List<string> mappingFolders = AppConfiguration.DatabaseMappingFile.Split(',').ToList();
                pManager.Configure(
                    mappingFolders.First()
                    , mappingFolders.Skip(1).ToList()
                    , AppConfiguration.DatabaseDialect
                    , AppConfiguration.DefaultApplicationConnection.ConnectionString);
            }
            else
            {
                pManager.Configure(
                    string.Format("{0}{1}", appRoot, "cfg\\")
                    , new List<string>() {string.Format("{0}{1}", appRoot, "bin\\")}
                    , AppConfiguration.DatabaseDialect
                    , AppConfiguration.DefaultApplicationConnection.ConnectionString);
            }

            if (AppConfiguration.CreateDatabase)
                pManager.ExportSchema();
            pManager.Start();
        }

        public readonly static List<AreaRegistration> Areas = new List<AreaRegistration>();

        public static void RegisterAreaInfo(AreaRegistration registrationInfo)
        {
            if (!Areas.Contains(registrationInfo))
                Areas.Add(registrationInfo);
        }

        public static List<string> GetAreaNames()
        {
            return (AppUtils.Areas.Select(p => p.AreaName).ToList());
        }

        public static List<Type> GetControllersByArea(AreaRegistration area)
        {
            //AreaRegistration area = Areas.Where(a => a.AreaName.Equals(areaName)).FirstOrDefault();
            List<Type> controllers = GetControllers();
            //controllers = controllers.Where(t => t.GetCustomAttributes(typeof(AreaAttribute), true).Count() > 0).ToList();
            controllers = (from c in controllers
                          from a in c.GetCustomAttributes(typeof(AreaAttribute), true).Cast<AreaAttribute>()
                          where (a.Registration.Equals(area.GetType()))
                            //&& (c.GetCustomAttributes(typeof(Display))
                          select c).ToList();
            return (controllers);
        }

        public static List<MethodInfo> GetActionsByController(Type controllerType)
        {
            List<MethodInfo> actions = controllerType.GetMethods().Where(p=>p.IsPublic).ToList();
            actions = ( from a in actions
                        //from ano in a.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).Cast<AreaAttribute>() // should be done in the security package
                        where (typeof(ActionResult).IsAssignableFrom(a.ReturnType))
                        select a
                       ).ToList();
            return (actions);
        }

        public static List<Type> GetControllers()
        {
            List<Type> controllers =
            AppUtils.ApplicationAssembly.GetTypes()
                    .Where(t =>
                        t != null
                        && t.IsPublic &&
                        t.Name.EndsWith("Controller", StringComparison.OrdinalIgnoreCase)
                        && !t.IsAbstract
                        && typeof(IController).IsAssignableFrom(t)
                    ).ToList();
            return (controllers);
        }
    }
}
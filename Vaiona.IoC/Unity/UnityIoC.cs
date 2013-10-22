using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using System.Configuration;
using Microsoft.Practices.Unity.Configuration;
using System.Web;

namespace Vaiona.IoC.Unity
{
    public class UnityIoC : IoCContainer
    {
        private const string sessionKey = "SessionLevelContainer";
        IUnityContainer container = null;
        List<UnityIoC> children = new List<UnityIoC>();

        public UnityIoC(IUnityContainer container)
        {
            this.container = container;
        }

        public UnityIoC(string configFilePath, string containerName, params object[] optionals)
        {
            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = configFilePath;
            Configuration config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            UnityConfigurationSection section = (UnityConfigurationSection)config.GetSection("unity");

            container = new UnityContainer();
            container.LoadConfiguration(section, containerName);

            //container = new UnityContainer();
            //container.LoadConfiguration(containerName);
        }

        public object Resolve(Type t)
        {
            return (container.Resolve(t));
        }

        //public IEnumerable<object> ResolveAll(Type t)
        //{
        //    IEnumerable<object> objects = container.ResolveAll(t);
        //    foreach (var item in children)
        //    {
        //        objects.Union(item.container.ResolveAll(t));
        //    }
        //    return (objects);
        //}

        public object Resolve<T>()
        {
            return (container.Resolve<T>());
        }

        //public IEnumerable<T> ResolveAll<T>()
        //{
        //    IEnumerable<T> objects = container.ResolveAll<T>();
        //    foreach (var item in children)
        //    {
        //        objects.Union(item.container.ResolveAll<T>());
        //    }
        //    return (objects);
        //}

        public void StartSessionLevelContainer()
        {
            UnityIoC child = new UnityIoC(container.CreateChildContainer());
            children.Add(child);
            HttpContext.Current.Session[sessionKey] = child;
        }

        public void ShutdownSessionLevelContainer()
        {
            UnityIoC child = HttpContext.Current.Session[sessionKey] as UnityIoC;
            children.Remove(child);
            child = null;
        }

        public void Teardown(object obj)
        {
            container.Teardown(obj);
        }

        public object ResolveForSession<T>()
        {
            object o = (HttpContext.Current.Session[sessionKey] as IoCContainer).Resolve<T>();
            return (o);
        }

        public object ResolveForSession(Type t)
        {
            object o = (HttpContext.Current.Session[sessionKey] as IoCContainer).Resolve(t);
            return (o);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.Unity;
using System.Configuration;
using Microsoft.Practices.Unity.Configuration;
using System.Web;
using System.Reflection;

namespace Vaiona.IoC.Unity
{
    public class UnityIoC : IoCContainer
    {
        private IUnityContainer container = null;
        private Dictionary<string, UnityIoC> children = new Dictionary<string, UnityIoC>();
        private Dictionary<Type, object> instances = new Dictionary<Type, object>();

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
        }

        public void StartSessionLevelContainer()
        {
            string key = HttpContext.Current.Session.SessionID;
            if (this.children.ContainsKey(key))
                return;
            UnityIoC child = new UnityIoC(container.CreateChildContainer());
            children.Add(key, child);
        }

        public void RegisterHeirarchical(Type from, Type to)
        {
            this.container.RegisterType(from, to, new HierarchicalLifetimeManager());
        }

        public void Register(Type from, Type to)
        {
            this.container.RegisterType(from, to, new TransientLifetimeManager());
        }

        public bool IsRegistered(Type t, string name)
        {
            return container.IsRegistered(t, name);
        }

        public object Resolve(Type t)
        {
            return (container.Resolve(t));
        }

        public T Resolve<T>()
        {
            return (container.Resolve<T>());
        }

        public T Resolve<T>(string name)
        {
            return (container.Resolve<T>(name));
        }

        public bool IsRegistered<T>(string name)
        {
            return container.IsRegistered<T>(name);
        }

        public void ShutdownSessionLevelContainer()
        {
            string key = HttpContext.Current.Session.SessionID;
            if (this.children.ContainsKey(key))
                children.Remove(key);
        }

        public void Teardown(object obj)
        {
            container.Teardown(obj);
        }

        public T ResolveForSession<T>()
        {
            string key = HttpContext.Current.Session.SessionID;
            if (this.children.ContainsKey(key))
            {
                UnityIoC child = this.children[key];
                try
                {
                    T o = child.container.Resolve<T>();
                    return (o);
                }
                catch (Exception ex)
                {
                    return default(T);
                }
            }
            return default(T);
        }

        public object ResolveForSession(Type t)
        {
            string key = HttpContext.Current.Session.SessionID;
            if (this.children.ContainsKey(key))
            {
                IoCContainer container = this.children[key];
                object o = container.Resolve(t);
                return (o);
            }
            return null;
        }
    }
}

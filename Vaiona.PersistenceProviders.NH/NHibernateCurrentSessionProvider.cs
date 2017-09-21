using System;
using System.Collections.Generic;
using System.Web;
using NHibernate;
using NHibernate.Context;
using NHibernate.Engine;
using System.Runtime.Remoting.Messaging;
using Vaiona.Utils.Cfg;

namespace Vaiona.PersistenceProviders.NH
{
    /// <summary>
    /// Taken from http://nhforge.org/blogs/nhibernate/archive/2011/03/03/effective-nhibernate-session-management-for-web-apps.aspx
    /// </summary>
    public class NHibernateCurrentSessionProvider : ICurrentSessionContext
    {
        private readonly ISessionFactoryImplementor _factory;
        public const string CURRENT_SESSION_CONTEXT_KEY = "NHibernateCurrentSessionFactory";

        public NHibernateCurrentSessionProvider(ISessionFactoryImplementor factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Retrieve the current session for the session factory.
        /// </summary>
        /// <returns></returns>
        public ISession CurrentSession() {
            ISession session;
            var currentSessionFactoryMap = GetCurrentFactoryMap();
            
            if (currentSessionFactoryMap == null ||
                !currentSessionFactoryMap.TryGetValue(_factory, out session)) {
                return null;
            }
            return (session);
        }

        /// <summary>
        /// Bind a new sessionInitializer to the context of the sessionFactory.
        /// </summary>
        /// <param name="sessionInitializer"></param>
        /// <param name="sessionFactory"></param>
        public static void Bind(ISession session, ISessionFactory sessionFactory) {
            var map = GetCurrentFactoryMap();
            map[sessionFactory] = session;
        }

        /// <summary>
        /// Unbind the current session of the session factory.
        /// </summary>
        /// <param name="sessionFactory"></param>
        /// <returns></returns>
        public static ISession UnBind(ISessionFactory sessionFactory) {
            if (sessionFactory == null)
                return null;
            var map = GetCurrentFactoryMap();
            if (!map.ContainsKey(sessionFactory))
                return null;
            ISession session = map[sessionFactory];
            map[sessionFactory] = null; // dereference the session object
            map.Remove(sessionFactory); // remove the map entry
            FactoryMapInContext = map;  // update the httpcontxt
            return (session);
        }

        /// <summary>
        /// Provides the CurrentMap of SessionFactories.
        /// If there is no map create/store and return a new one.
        /// </summary>
        /// <returns></returns>
        private static IDictionary<ISessionFactory, ISession> GetCurrentFactoryMap() {
            var currentFactoryMap = FactoryMapInContext;

            if (currentFactoryMap == null) {
                currentFactoryMap = new Dictionary<ISessionFactory, ISession>();
                FactoryMapInContext = currentFactoryMap;
            }

            return currentFactoryMap;
        }

        private static IDictionary<ISessionFactory, ISession> FactoryMapInContext {
            get {
                if (AppConfiguration.IsWebContext) {
                    return HttpContext.Current.Items[CURRENT_SESSION_CONTEXT_KEY] as IDictionary<ISessionFactory, ISession>;
                }
                else {
                    return CallContext.GetData(CURRENT_SESSION_CONTEXT_KEY) as IDictionary<ISessionFactory, ISession>;
                }
            }
            set {
                if (AppConfiguration.IsWebContext)
                {
                    HttpContext.Current.Items[CURRENT_SESSION_CONTEXT_KEY] = value;
                }
                else {
                    CallContext.SetData(CURRENT_SESSION_CONTEXT_KEY, value);
                }
            }
        }
    }
}
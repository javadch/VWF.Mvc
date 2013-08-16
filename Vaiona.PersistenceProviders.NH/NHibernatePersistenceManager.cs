using System.Web;
using NHibernate;
using NHibernate.Cfg;
using NHibernate.Tool.hbm2ddl;
using System.Diagnostics.Contracts;
using System.IO;
using System;
using System.Text.RegularExpressions;
using Vaiona.Persistence.Api;
using NHibernate.Context;
using System.Collections.Generic;
using System.Linq;
using Vaiona.Util.Cfg;

namespace Vaiona.PersistenceProviders.NH
{
    public class NHibernatePersistenceManager : IPersistenceManager
    {
        private static ISessionFactory sessionFactory;
        private static Configuration cfg;
        //private static string configFile = "";

        private List<string> componentFolders = new List<string>();
        private List<string> moduleFolders = new List<string>();
        
        public void Configure(string connectionString = "", string databaseDilect = "DB2Dialect", bool useNeutralMapping = false)
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(databaseDilect));

            if (sessionFactory != null)
                return;

            string configFileName = string.Format(@"{0}.hibernate.cfg.xml", databaseDilect);
            string configFileFullPath = Path.Combine(AppConfiguration.WorkspaceGeneralRoot, "Db", "Settings", configFileName);
            cfg = new Configuration();
            cfg.Configure(configFileFullPath);

            //  Tells NHibernate to use the provided class as the current session provider (CurrentSessionContextClass). This way the sessionFactory.GetCurrentSession
            // will call the CurrentSession method of this class.
            cfg.Properties[NHibernate.Cfg.Environment.CurrentSessionContextClass] = typeof(NHibernateCurrentSessionProvider).AssemblyQualifiedName;

            // in case of having specific queries or mappings for different dialects, it is better (and possible) 
            // to develop different mapping files and externalizing queries
            registerComponentMappings(cfg, useNeutralMapping ? "Default" : databaseDilect);
            registerModuleMappings(cfg, useNeutralMapping ? "Default" : databaseDilect);

            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                cfg.SetProperty(NHibernate.Cfg.Environment.ConnectionString, connectionString);
                //cfg.SetProperty(NHibernate.Cfg.Environment.ConnectionString, Util.Decrypt(connectionString));
            }

            Contract.Ensures(cfg != null);
        }

        public void ExportSchema(bool generateScript = false, bool executeAgainstTargetDB = true, bool justDrop = false)
        {
            // think of installing a module separately: export that module to DB, add entries to cfg., restart cfg and session factory, etc.
            new SchemaExport(cfg).Execute(generateScript, executeAgainstTargetDB, justDrop);
            foreach (string comDir in componentFolders)
            {
                string postInstallationScript = Path.Combine(comDir, "Db", "PostInstallationScript.sql");
                if (File.Exists(postInstallationScript))
                {
                    executePostInstallationScript(postInstallationScript);
                }
            }
            foreach (string modDir in moduleFolders)
            {
                string postInstallationScript = Path.Combine(modDir, "Db", "PostInstallationScript.sql");
                if (File.Exists(postInstallationScript))
                {
                    executePostInstallationScript(postInstallationScript);
                }
            }
        }

        private void executePostInstallationScript(string postInstallationScript)
        {
            string sql;

            using (FileStream strm = File.OpenRead(postInstallationScript))
            {
                var reader = new StreamReader(strm);
                sql = reader.ReadToEnd();
            }

            var regex = new Regex("^GO", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string[] lines = regex.Split(sql);

            this.Start();
            ISession session = sessionFactory.OpenSession();
            session.BeginTransaction();
            try
            {
                foreach (string line in lines)
                {
                    IQuery query = session.CreateSQLQuery(line);
                    query.ExecuteUpdate();
                }
                session.Transaction.Commit();
            }
            catch
            {
                session.Transaction.Rollback();
            }
            finally
            {
                this.Shutdown();
            }
        }
        
        public void Start()
        {
            // may need locking for concurrent calls!
            if (sessionFactory != null)
                return;
            sessionFactory = cfg.BuildSessionFactory();

            Contract.Ensures(sessionFactory != null);
        }

        public void Shutdown()
        {
            EndContext();
            // may need locking for concurrent calls!
            if (sessionFactory != null)
            {
                sessionFactory.Close();
            }
        }

        public IUnitOfWork CreateUnitOfWork(bool autoCommit = false, bool throwExceptionOnError = true, bool allowMultipleCommit = false
            , EventHandler beforeCommit = null, EventHandler afterCommit = null, EventHandler beforeIgnore = null, EventHandler afterIgnore = null)
        {
            ISession session = getSession();
            NHibernateUnitOfWork u = new NHibernateUnitOfWork(this, session, autoCommit, throwExceptionOnError, allowMultipleCommit);
            u.BeforeCommit += beforeCommit;
            u.AfterCommit += afterCommit;
            u.BeforeIgnore += beforeIgnore;
            u.AfterIgnore += afterIgnore;
            return (u);
        }

        public object GetCurrentConversation()
        {
            return(getSession());
        }

        public void StartConversation()
        {
            foreach (var sessionFactory in getSessionFactories())
            {
                var localFactory = sessionFactory;

                NHibernateCurrentSessionProvider.Bind(new Lazy<ISession>(() => beginSession(localFactory)), sessionFactory);
            }
        }

        public void ShutdownConversation()
        {
            foreach (var sessionfactory in getSessionFactories())
            {
                var session = NHibernateCurrentSessionProvider.UnBind(sessionfactory);
                if (session == null) continue;
                endSession(session, false);
            }
        }

        public void EndConversation()
        {
            foreach (var sessionfactory in getSessionFactories())
            {
                var session = NHibernateCurrentSessionProvider.UnBind(sessionfactory);
                if (session == null) continue;
                endSession(session, true);
            }
        }

        public void EndContext()
        {
            foreach (var sessionfactory in getSessionFactories())
            {
                var session = NHibernateCurrentSessionProvider.UnBind(sessionfactory);
                if (session == null) continue;
                endSession(session, false);
            }
        }

        private ISession getSession()
        {
            ISession session = null;
            try
            {
                session = sessionFactory.GetCurrentSession();
            }
            catch
            { }

            if (session == null)
            {   //start a new session
                StartConversation();
                // try get the session after starting a new conversation
                session = sessionFactory.GetCurrentSession();
            }
            return (session);
        }

        /// <summary>
        /// Retrieves all ISessionFactory instances via IoC
        /// </summary>
        private IEnumerable<ISessionFactory> getSessionFactories()
        {
            var sessionFactories = new List<ISessionFactory>() { sessionFactory };

            if (sessionFactories == null || !sessionFactories.Any())
                throw new TypeLoadException("There should be at least one ISessionFactory registered");
            return sessionFactories;
        }

        private static ISession beginSession(ISessionFactory sessionFactory)
        {
            var session = sessionFactory.OpenSession();
            session.BeginTransaction(System.Data.IsolationLevel.ReadCommitted);
            return session;
        }

        private static void endSession(ISession session, bool commitTransaction = false)
        {
            try
            {
                if (session.Transaction != null && session.Transaction.IsActive)
                {
                    if (commitTransaction)
                    {
                        try
                        {
                            session.Transaction.Commit();
                        }
                        catch
                        {
                            session.Transaction.Rollback();
                            throw;
                        }
                    }
                    else
                    {
                        session.Transaction.Rollback();
                    }
                }
            }
            finally
            {
                if (session.IsOpen)
                    session.Close();

                session.Dispose();
            }
        }

        private void registerModuleMappings(Configuration cfg, string dialect)
        {
            //check the modules' statuses with the module registration system and register the mappings just for valid ones
            //throw new NotImplementedException();
            // add module folders to the list
        }

        /// <summary>
        /// Any component dealing with data should have a Db folder in its workspace folder containing the Mappings folder
        /// If the components accesses data through other components' APIs there is no need to provide the mapping again
        /// </summary>
        /// <param name="cfg"></param>
        private void registerComponentMappings(Configuration cfg, string dialect)
        {
            string componentsRoot = AppConfiguration.WorkspaceComponentRoot;
            if (!Directory.Exists(componentsRoot))
                return;
            foreach (string comDir in Directory.GetDirectories(componentsRoot))
            {
                string mappingFolder = Path.Combine(comDir, "Db", "Mappings", dialect);
                if (Directory.Exists(mappingFolder))
                {
                    cfg.AddDirectory(new DirectoryInfo(mappingFolder));
                    componentFolders.Add(comDir);
                }
            }
        }
    }
}

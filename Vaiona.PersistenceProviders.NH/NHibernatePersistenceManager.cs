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

        public object Factory { get { return sessionFactory; } }

        Dictionary<string, List<FileInfo>> componentPostInstallationFiles = new Dictionary<string, List<FileInfo>>();
        Dictionary<string, List<FileInfo>> modulePostInstallationFiles = new Dictionary<string, List<FileInfo>>();
        
        public void Configure(string connectionString = "", string databaseDilect = "DB2Dialect", string fallbackFoler = "Default")
        {
            Contract.Requires(!string.IsNullOrWhiteSpace(databaseDilect));

            if (sessionFactory != null)
                return;

            string configFileName = string.Format(@"{0}.hibernate.cfg.xml", databaseDilect);
            string configFileFullPath = Path.Combine(AppConfiguration.WorkspaceGeneralRoot, "Db", "Settings", configFileName);
            cfg = new Configuration();
            cfg.Configure(configFileFullPath);
#if DEBUG
            cfg.SetInterceptor(new NHInterceptor());
#endif
            //  Tells NHibernate to use the provided class as the current session provider (CurrentSessionContextClass). This way the sessionFactory.GetCurrentSession
            // will call the CurrentSession method of this class.
            cfg.Properties[NHibernate.Cfg.Environment.CurrentSessionContextClass] = typeof(NHibernateCurrentSessionProvider).AssemblyQualifiedName;

            // in case of having specific queries or mappings for different dialects, it is better (and possible) 
            // to develop different mapping files and externalizing queries
            registerMappings(cfg, fallbackFoler, databaseDilect, AppConfiguration.WorkspaceComponentRoot, ref componentPostInstallationFiles);
            registerMappings(cfg, fallbackFoler, databaseDilect, AppConfiguration.WorkspaceModulesRoot, ref modulePostInstallationFiles);

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
            foreach (var component in componentPostInstallationFiles)
            {
                foreach (var file in component.Value)
                {
                    executePostInstallationScript(file);
                }
            }
            foreach (var module in modulePostInstallationFiles)
            {
                foreach (var file in module.Value)
                {
                    executePostInstallationScript(file);
                }
            }           
        }

        private void executePostInstallationScript(FileInfo postInstallationScript)
        {
            string sql;

            using (FileStream strm = postInstallationScript.OpenRead())
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
            //this flush mode will flush on manual flushes and when transactions are committed.
            session.FlushMode = FlushMode.Commit;
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
#if DEBUG
            var session = sessionFactory.OpenSession(cfg.Interceptor);
#else
            var session = sessionFactory.OpenSession();
#endif
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
                            if(session.IsDirty())
                                session.Transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            session.Transaction.Rollback();
                            throw new Exception("There were some changes submitted to the system, but could not be committed!", ex);
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

        /// <summary>
        /// Any component dealing with data should have a Db folder in its workspace folder containing the Mappings folder
        /// If the components accesses data through other components' APIs there is no need to provide the mapping again
        /// </summary>
        /// <param name="cfg"> the NH configuration object that the mapping files are registered with it.</param>
        /// <param name="fallbackFoler">The folder than contains default and DBMS neutral mapping files</param>
        /// <param name="dialect">The specific DBMS dialect to be used in the current configuration</param>
        /// <param name="componentOrModulePath">if module: this is the root folder of all the modules. if component, the root folder containing all the components</param>
        /// <param name="post">holds a reference to the post installation files compiled from merging of the PostObjects folder of the fallback and dialect folders</param>
        private void registerMappings(Configuration cfg, string fallbackFoler, string dialect, string componentOrModulePath, ref Dictionary<string, List<FileInfo>> post)
        {
            if (!Directory.Exists(componentOrModulePath))
                return;
            DirectoryInfo rootDir = new DirectoryInfo(componentOrModulePath);
            foreach (DirectoryInfo moduleOrComponentDir in rootDir.GetDirectories())
            {
                List<FileInfo> mappingFiles = compileMappingFileList(moduleOrComponentDir, fallbackFoler, dialect, ref post);
                mappingFiles.ForEach(p => cfg.AddFile(p));
            }
        }

        /// <summary>
        /// takes a component or a module, extracts the mapping files from the fallback and dialect directories and merges them by overwriting the fallback ones by their dialect's counterparts if exists.
        /// The function also do the same for post installation files.
        /// </summary>
        /// <param name="comDir">The module or component root folder</param>
        /// <param name="fallbackFolerName">the name of the fallback folder. It is not mandatory to provide a fallback folder if it doesn't apply</param>
        /// <param name="dialectName">the name of the dialect, should be same as the dialect folder name. It is not mandatory to provide a dialect folder if it doesn't apply, i.e. when there is nothing specific to the dialect</param>
        /// <param name="post">the reference to the post installation files list</param>
        /// <returns>the merged mapping files list created from fallback and/ or dialect folders</returns>
        private List<FileInfo> compileMappingFileList(DirectoryInfo comDir, string fallbackFolerName, string dialectName, ref Dictionary<string, List<FileInfo>> post)
        {            
            string fallbackFolderPath = Path.Combine(comDir.FullName, "Db", "Mappings", fallbackFolerName);
            string dialectFolderPath = Path.Combine(comDir.FullName, "Db", "Mappings", dialectName);

            Dictionary<string, FileInfo> compiledList = getMappingsFrom(fallbackFolderPath);
            Dictionary<string, FileInfo> dialectList = getMappingsFrom(dialectFolderPath);
            //merge the lists into the compiledList
            foreach (var item in dialectList)
            {
                if (compiledList.ContainsKey(item.Key))
                {
                    compiledList[item.Key] = item.Value;
                }
                else
                {
                    compiledList.Add(item.Key, item.Value);
                }
            }

            List<FileInfo> fallbackPost = getPostInstallationInfo(fallbackFolderPath);
            List<FileInfo> dialectPost = getPostInstallationInfo(dialectFolderPath);
            List<FileInfo> compiledPost = fallbackPost.ToList();
            foreach (var item in dialectPost)
            {
                var dup = fallbackPost.Where(p => p.Name.Equals(item.Name)).FirstOrDefault();
                if (dup != null) // the dialect has overwritten the fallback
                {
                    compiledPost.Remove(dup);
                }
                // the dialect has added a file                                
                compiledPost.Add(item);
            }
            post.Add(comDir.Name, compiledPost);
            return (compiledList.Values.ToList());
        }

        private List<FileInfo> getPostInstallationInfo(string mappingPath)
        {
            if (Directory.Exists(mappingPath))
            {
                DirectoryInfo postObjectsDir = (new DirectoryInfo(mappingPath)).GetDirectories().Where(p => p.Name.Equals("PostObjects")).FirstOrDefault();
                if (postObjectsDir != null)
                {
                    return (postObjectsDir.GetFiles().Where(p => p.Name.EndsWith(".sql")).ToList());
                }
            }
            return (new List<FileInfo>());
        }

        /// <summary>
        /// Iterates over all first level child folders to obtain all the mapping files having .bhm.xml extension.
        /// When obtained, arranges them in a dictionary, in that the key is constructed from the holding folder name followed by the file name.
        /// The key is used for later overwriting by dialect's provided files.
        /// </summary>
        /// <param name="mappingPath">is the full path to a fallback or a dialect mapping folder for a specific component or module</param>
        /// <returns>all mapping files in the subfolders of mappingPath in a dictionary</returns>
        private Dictionary<string, FileInfo> getMappingsFrom(string mappingPath)
        {
            Dictionary<string, FileInfo> fileList = new Dictionary<string, FileInfo>();
            if (Directory.Exists(mappingPath))
            {
                // go through all the folders in the mapping container and add them to the mapping file list, expect for the PostObjects folder
                // which is a specific folder designed to contain post installation scripts
                DirectoryInfo mappingRootDir = new DirectoryInfo(mappingPath);
                
                foreach (var mappingContainerDir in mappingRootDir.GetDirectories().Where(p=> !p.Name.Equals("PostObjects")))
                {
                    mappingContainerDir.GetFiles().Where(p => p.Name.EndsWith(".hbm.xml")) //filter just the valid mapping files
                        .ToList().ForEach(p =>
                        fileList.Add(string.Format("{0}.{1}", mappingContainerDir.Name, p.Name), p)); // Key: provides a uniqueness control inside the component/ module, required for overwriting procedure
                }
            }
            //else // there is no mapping folder, so expect all the mappings to be in the dialect folder or no mapping at all            
            return (fileList);
        }
    }
}

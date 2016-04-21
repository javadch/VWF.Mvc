using NHibernate;
using NHibernate.Cfg;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaiona.Persistence.Api;
using Vaiona.Utils.Cfg;

namespace Vaiona.PersistenceProviders.NH
{
    public class Conversation
    {
        static Dictionary<object, List<IUnitOfWork>> attachedUnits = new Dictionary<object, List<IUnitOfWork>>(); // special observer pattern
        private ISessionFactory sessionFactory;
        private Configuration cfg;
        private bool showQueries = false;
        private bool commitTransaction = false;
        ISession session = null;
        IStatelessSession stSession = null;
        bool statefull = true;
        bool alive = false;
        public Conversation(ISessionFactory sessionFactory, Configuration cfg, bool commitTransaction = false, bool showQueries = false)
        {
            this.sessionFactory = sessionFactory;
            this.cfg = cfg;
            this.showQueries = showQueries;
            this.commitTransaction = commitTransaction;
        }

        public Conversation(ISession session, ISessionFactory sessionFactory, Configuration cfg, bool commitTransaction = false, bool showQueries = false)
            : this(sessionFactory, cfg, commitTransaction, showQueries)
        {
            this.session = session;
            alive = true;
            statefull = true;
            stSession = null;
        }

        public Conversation(IStatelessSession session, ISessionFactory sessionFactory, Configuration cfg, bool commitTransaction = false, bool showQueries = false)
            : this(sessionFactory, cfg, commitTransaction, showQueries)
        {
            this.stSession = session;
            alive = true;
            statefull = false;
            session = null;
        }
        public bool IsStatefull()
        {
            return statefull;
        }

        public ISession GetSession()
        {
            if (statefull)
                return session;
            return null;
        }

        public IStatelessSession GetStatelessSession()
        {
            if (!statefull)
                return stSession;
            return null;
        }

        public void Start(IUnitOfWork uow)
        {
            if (!alive && this.session == null)
            {
                session = sessionFactory.OpenSession(cfg.Interceptor);
                alive = true;
            }
            // if uow is already there, its a wrong call pattern! a unit of work is trying to start the conversation more than once!!
            registerUnit(session, uow);
            if (!AppConfiguration.CacheQueryResults)
                session.CacheMode = CacheMode.Ignore;
            else
                session.CacheMode = CacheMode.Normal;
            if (!session.Transaction.IsActive)
                session.Transaction.Begin(System.Data.IsolationLevel.ReadCommitted);
            if (showQueries)
                Trace.WriteLine("SQL output at:" + DateTime.Now.ToString() + "--> " + "A conversation was opened. ID: " + session.GetHashCode());
            stSession = null;
        }

        public void StartStateless(IUnitOfWork uow)
        {
            if (!alive && this.stSession == null)
            {
                stSession = sessionFactory.OpenStatelessSession();
                alive = true;
            }
            // if uow is already there, its a wrong call pattern! a unit of work is trying to start the conversation more than once!!
            registerUnit(stSession, uow);
            if (!stSession.Transaction.IsActive)
                stSession.Transaction.Begin(System.Data.IsolationLevel.ReadCommitted);
            if (showQueries)
                Trace.WriteLine("SQL output at:" + DateTime.Now.ToString() + "--> " + "A conversation was opened. ID: " + stSession.GetHashCode());
            this.session = null;
            statefull = false;
        }

        public void Restart(IUnitOfWork uow)
        {
            if(statefull)
            {
                if (!attachedUnits[session].Contains(uow)) // UoW is not listed so not allowd to restart
                    return; //maybe an exception would be better
                if (session == null)
                    this.Start(uow);
                session.Transaction.Begin(System.Data.IsolationLevel.ReadCommitted);
            }
            else
            {
                if (!attachedUnits[stSession].Contains(uow)) // UoW is not listed so not allowd to restart
                    return; //maybe an exception would be better
                if (stSession == null)
                    this.StartStateless(uow);
                stSession.Transaction.Begin(System.Data.IsolationLevel.ReadCommitted);
            }

        }

        public void End(IUnitOfWork uow)
        {
            if (statefull)
            {
                if (!attachedUnits.ContainsKey(session) || !attachedUnits[session].Contains(uow))
                    return; // the UoW is not authorized to end the conversation
                unRegisterUnit(session, uow); // remove the UoW from the list of conversation observers
                if (!attachedUnits.ContainsKey(session)) // there is no observer, so it's safe to close and collect the session/ resources
                {
                    endSession();
                }
            }
            else
            {
                if (!attachedUnits.ContainsKey(stSession) || !attachedUnits[session].Contains(uow))
                    return; // the UoW is not authorized to end the conversation
                unRegisterUnit(stSession, uow); // remove the UoW from the list of conversation observers
                if (!attachedUnits.ContainsKey(stSession)) // there is no observer, so it's safe to close and collect the session/ resources
                {
                    endStatelessSession();
                }
            }
        }

        public void Terminate()
        {
            if (statefull)
            {
                if (!attachedUnits.ContainsKey(session))
                    return;
                attachedUnits.Remove(session); // remove the session and all its UoWs
                if (!attachedUnits.ContainsKey(session)) // there is no observer, so it's safe to close and collect the session/ resources
                {
                    endSession();
                }
            }
            else
            {
                if (!attachedUnits.ContainsKey(stSession))
                    return; 
                attachedUnits.Remove(stSession); 
                if (!attachedUnits.ContainsKey(stSession)) // there is no observer, so it's safe to close and collect the session/ resources
                {
                    endStatelessSession();
                }
            }
        }

        public static void Dereference(object session)
        {
            attachedUnits.Remove(session); // remove the session and all its UoWs
            //if (session is ISession)
            //{

            //} else if(session is IStatelessSession)
            //{

            //}
        }

        public void Clear(bool applyChanages = false)
        {
            if (statefull)
            {
                if (applyChanages)
                    session.Flush();
                session.Clear();
            }
        }

        private void endSession()
        {
            try
            {
                if (session.Transaction != null && session.Transaction.IsActive)
                {
                    if (commitTransaction)
                    {
                        try
                        {
                            if (session.IsDirty())
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
                if (showQueries)
                    Trace.WriteLine("SQL output at:" + DateTime.Now.ToString() + "--> " + "A conversation was closed. ID: " + session.GetHashCode());
                session.Dispose();
                GC.Collect();
                session = null;
            }
        }

        private void endStatelessSession()
        {
            try
            {
                if (stSession.Transaction != null && stSession.Transaction.IsActive)
                {
                    if (commitTransaction)
                    {
                        try
                        {
                            if (stSession.Transaction.IsActive)
                                stSession.Transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            stSession.Transaction.Rollback();
                            throw new Exception("There were some changes submitted to the system, but could not be committed!", ex);
                        }
                    }
                    else
                    {
                        stSession.Transaction.Rollback();
                    }
                }
            }
            finally
            {
                if (stSession.IsOpen)
                    stSession.Close();
                if (showQueries)
                    Trace.WriteLine("SQL output at:" + DateTime.Now.ToString() + "--> " + "A conversation was closed. ID: " + stSession.GetHashCode());
                stSession.Dispose();
                stSession = null;
                GC.Collect();
            }
        }

        private void registerUnit(object session, IUnitOfWork uow)
        {
            if(!attachedUnits.ContainsKey(session))
            {
                attachedUnits.Add(session, new List<IUnitOfWork> { uow });
            }
            else if(!attachedUnits[session].Contains(uow))
            {
                attachedUnits[session].Add(uow);
            }
        }

        private void unRegisterUnit(object session, IUnitOfWork uow)
        {
            if (!attachedUnits.ContainsKey(session))
                return;
            attachedUnits[session].Remove(uow);
            if (attachedUnits[session].Count() <= 0)
                attachedUnits.Remove(session);            
        }
    }
}

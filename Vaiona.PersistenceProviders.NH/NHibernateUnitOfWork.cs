using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using Vaiona.Persistence.Api;
using NHibernate.Context;
using System.Web;
using System.Diagnostics.Contracts;
using System.Data.SqlClient;
using System.Data;

namespace Vaiona.PersistenceProviders.NH
{
    public class NHibernateUnitOfWork: IUnitOfWork
    {
        //internal ISession Session = null;
        private bool autoCommit = false;
        private bool throwExceptionOnError = true;
        private bool allowMultipleCommit = true;
        internal Conversation Conversation = null;

        public IPersistenceManager PersistenceManager { get; internal set; }
        internal NHibernateUnitOfWork(NHibernatePersistenceManager persistenceManager, Conversation conversation, bool autoCommit = false, bool throwExceptionOnError = true)
        {
            this.PersistenceManager = persistenceManager;
            this.autoCommit = autoCommit;
            this.throwExceptionOnError = throwExceptionOnError;
            this.Conversation = conversation;
            this.Conversation.Start(this);
        }

#if DEBUG
        public ISession Session
        {
            get
            {
                return this.Conversation.GetSession();
            }
        }
#else
        internal ISession Session
        {
            get
            {
                return this.Conversation.GetSession();
            }
        }
#endif

        public IReadOnlyRepository<TEntity> GetReadOnlyRepository<TEntity>(Vaiona.Persistence.Api.CacheMode cacheMode = Vaiona.Persistence.Api.CacheMode.Ignore) where TEntity : class
        {
            IReadOnlyRepository<TEntity> repo = new NHibernateReadonlyRepository<TEntity>(this, cacheMode);
            return (repo);
        }

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            IRepository<TEntity> repo = new NHibernateRepository<TEntity>(this);
            return (repo);
        }

        public void ClearCache(bool applyChanages = true)
        {
            this.Conversation.Clear(applyChanages);
        }

        public void Commit()
        {
            try
            {
                if (BeforeCommit != null)
                    BeforeCommit(this, EventArgs.Empty);
                // try detect what is going to be committed, adds, deletes, changes, and log some information about them after commit is done!                

                Session.Transaction.Commit();
                
                if (Session.Transaction.WasCommitted)
                {
                    // log the changes detected in previous steps
                    if (AfterCommit != null)
                        AfterCommit(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                //session.Transaction.Rollback(); //??
                if (throwExceptionOnError)
                    throw ex;
            }
        }

        public void Ignore()
        {
            try
            {
                if (Session.Transaction.IsActive)
                {
                    if (BeforeIgnore != null)
                        BeforeIgnore(this, EventArgs.Empty);
                    Session.Transaction.Rollback();
                    if (Session.Transaction.WasRolledBack)
                    {
                        if (AfterIgnore != null)
                            AfterIgnore(this, EventArgs.Empty);
                    }
                }
            }
            catch (Exception ex)
            {
                if (throwExceptionOnError)
                    throw ex;
            }
        }

        public T Execute<T>(string queryName, Dictionary<string, object> parameters = null)
        {
            if (parameters != null && parameters.Any(p => p.Value == null))
                throw new ArgumentException("The parameter array has a null element", "parameters");

            T result = default(T);
            ISession session = this.Conversation.GetSession();
            try
            {
                //session.BeginTransaction();
                IQuery query = session.GetNamedQuery(queryName);
                if (parameters != null)
                {
                    foreach (var item in parameters)
                    {
                        query.SetParameter(item.Key, item.Value);
                    }
                }
                result = query.UniqueResult<T>();
                //session.Transaction.Commit();
            }
            catch
            {
                //session.Transaction.Rollback();
                throw new Exception(string.Format("Failed for execute named query '{0}'.", queryName));
            }
            finally
            {
                // Do Nothing
            }
            return result;
        }

        public T ExecuteDynamic<T>(string queryString, Dictionary<string, object> parameters = null)
        {
            if (parameters != null && parameters.Any(p => p.Value == null))
                throw new ArgumentException("The parameter array has a null element", "parameters");

            T result = default(T);
            ISession session = this.Conversation.GetSession();
            try
            {
                //session.BeginTransaction();
                IQuery query = session.CreateSQLQuery(queryString);
                if (parameters != null)
                {
                    foreach (var item in parameters)
                    {
                        query.SetParameter(item.Key, item.Value);
                    }
                }
                result = query.UniqueResult<T>();
                //session.Transaction.Commit();
            }
            catch
            {
                //session.Transaction.Rollback();
                throw new Exception(string.Format("Failed for execute the submitted native query."));
            }
            finally
            {
                // Do Nothing
            }
            return result;
        }

        public int ExecuteNonQuery(string queryString, Dictionary<string, object> parameters = null)
        {            
            if (parameters != null && parameters.Any(p => p.Value == null))
                throw new ArgumentException("The parameter array has a null element", "parameters");
            int result = 0;
            try
            {
                using (ITransaction transaction = this.Session.BeginTransaction())
                {
                    IDbCommand command = this.Session.Connection.CreateCommand();
                    command.Connection = this.Session.Connection;

                    transaction.Enlist(command);

                    command.CommandText = queryString;
                    if (parameters != null)
                    {
                        foreach (var item in parameters)
                        {
                            command.Parameters.Add(new SqlParameter(item.Key, item.Value)); // sql paramater must be changed to a more generic one
                        }
                    }
                    command.ExecuteNonQuery();

                    transaction.Commit();
                }
            }
            catch
            {
                //session.Transaction.Rollback();
                throw new Exception(string.Format("Failed for execute the submitted native query."));
            }
            finally
            {
                // Do Nothing
            }
            return result;
        }

        public object ExecuteScalar(string queryString, Dictionary<string, object> parameters = null)
        {
            if (parameters != null && parameters.Any(p => p.Value == null))
                throw new ArgumentException("The parameter array has a null element", "parameters");
            object result = null;
            try
            {
                using (ITransaction transaction = this.Session.BeginTransaction())
                {
                    IDbCommand command = this.Session.Connection.CreateCommand();
                    command.Connection = this.Session.Connection;

                    transaction.Enlist(command);

                    command.CommandText = queryString;
                    if (parameters != null)
                    {
                        foreach (var item in parameters)
                        {
                            command.Parameters.Add(new SqlParameter(item.Key, item.Value)); // sql paramater must be changed to a more generic one
                        }
                    }
                    result = command.ExecuteScalar();

                    transaction.Commit();
                }
            }
            catch
            {
                //session.Transaction.Rollback();
                throw new Exception(string.Format("Failed for execute the submitted native query."));
            }
            finally
            {
                // Do Nothing
            }
            return result;
        }

        public DataTable ExecuteQuery(string queryString, Dictionary<string, object> parameters = null)
        {
            if (parameters != null && parameters.Any(p => p.Value == null))
                throw new ArgumentException("The parameter array has a null element", "parameters");
            DataTable table = new DataTable();
            try
            {
                using (IDbCommand command = this.Session.Connection.CreateCommand())
                {
                    command.Connection = this.Session.Connection;
                    command.CommandText = queryString;

                    if (parameters != null)
                    {
                        foreach (var item in parameters)
                        {
                            command.Parameters.Add(new SqlParameter(item.Key, item.Value)); // sql paramater must be changed to a more generic, IDbParameter
                        }
                    }

                    IDataReader dr = command.ExecuteReader(CommandBehavior.SingleResult);
                    table.Load(dr);
                }
            }
            catch (Exception ex)
            {
                //session.Transaction.Rollback();
                throw new Exception(string.Format("Failed for execute the submitted native query."), ex);
            }
            finally
            {
                // Do Nothing
            }
            return table;
        }

        private bool isDisposed = false;
        ~NHibernateUnitOfWork()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            // If you need thread safety, use a lock around these  
            // operations, as well as in your methods that use the resource. 
            try
            {
                if (!isDisposed)
                {
                    if (disposing)
                    {
                        disposeResources();
                        isDisposed = true;
                    }
                }
            }
            catch
            { // do nothing
            }
        }
        private void disposeResources()
        {
            if (Session == null)
                return;
            if (autoCommit & !Session.Transaction.WasCommitted)
                this.Commit();
            else
                this.Ignore();
            // Do not close the session, as it is usually shared between multiple units of work in a single HTTP request context. The conversation object takes care of it.
            this.Conversation.End(this);
            // unhook the event handlers appropriately
            BeforeCommit = null;
            AfterCommit = null;
            BeforeIgnore = null;
            AfterIgnore = null;
        }

        public event EventHandler BeforeCommit;

        public event EventHandler AfterCommit;

        public event EventHandler BeforeIgnore;

        public event EventHandler AfterIgnore;
    }
}

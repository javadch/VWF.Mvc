using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using Vaiona.Persistence.Api;
using NHibernate.Context;
using System.Web;

namespace Vaiona.PersistenceProviders.NH
{
    public class NHibernateBulkUnitOfWork: IUnitOfWork
    {
        internal IStatelessSession Session = null;
        private bool autoCommit = false;
        private bool throwExceptionOnError = true;
        private bool allowMultipleCommit = true;

        public IPersistenceManager PersistenceManager { get; internal set; }
        internal NHibernateBulkUnitOfWork(NHibernatePersistenceManager persistenceManager, IStatelessSession session, bool autoCommit = false, bool throwExceptionOnError = true, bool allowMultipleCommit = false)
        {
            this.PersistenceManager = persistenceManager;
            this.autoCommit = autoCommit;
            this.throwExceptionOnError = throwExceptionOnError;
            this.allowMultipleCommit = allowMultipleCommit;
            this.Session = session;
            this.Session.BeginTransaction();
        }

        public IReadOnlyRepository<TEntity> GetReadOnlyRepository<TEntity>() where TEntity : class
        {
            IReadOnlyRepository<TEntity> repo = new NHibernateReadonlyRepository<TEntity>(this);
            return (repo);
        }

        public IRepository<TEntity> GetRepository<TEntity>() where TEntity : class
        {
            IRepository<TEntity> repo = new NHibernateRepository<TEntity>(this);
            return (repo);
        }

        public void ClearCache()
        {
            //Session.
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
                }
                if (AfterCommit != null)
                    AfterCommit(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                //session.Transaction.Rollback(); //??
                if (throwExceptionOnError)
                    throw;
            }
            if (allowMultipleCommit && !this.Session.Transaction.IsActive)
            {
                this.Session.Transaction.Begin();
            }
            //else
            //{
            //    Session.Close();
            //}
        }

        //public void CommitAndContinue()
        //{
        //    Commit();
        //    this.session.Transaction.Begin();
        //}

        public void Ignore()
        {
            try
            {
                if (Session.Transaction.IsActive)
                {
                    if (BeforeIgnore != null)
                        BeforeIgnore(this, EventArgs.Empty);
                    Session.Transaction.Rollback();
                    if (AfterIgnore != null)
                        AfterIgnore(this, EventArgs.Empty);
                    if (Session.Transaction.WasRolledBack)
                    {
                        // log
                    }
                }
            }
            catch (Exception ex)
            {
                if (throwExceptionOnError)
                    throw;
            }
            if (allowMultipleCommit && !this.Session.Transaction.IsActive)
            {
                this.Session.Transaction.Begin();
            }
        }

        //public void IgnoreAndContinue()
        //{
        //    Ignore();
        //    this.session.Transaction.Begin();
        //}

        private bool isDisposed = false;
        ~NHibernateBulkUnitOfWork()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            // If you need thread safety, use a lock around these  
            // operations, as well as in your methods that use the resource. 
            if (!isDisposed)
            {
                if (disposing)
                {
                    disposeResources();
                    isDisposed = true;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void disposeResources()
        {
            if (autoCommit & !Session.Transaction.WasCommitted)
                this.Commit();
            else
                this.Ignore();
            //CurrentSessionContext.Unbind(this.Session.SessionFactory);
            // Do not close the session, as it is usually shared between multiple units of work in a single HTTP request context
            //if (Session.IsOpen)
            //    Session.Close();
            //Session.Dispose();
            Session = null; // dereference the pointer to the shared session object
            //HttpContext.Current.Session.Remove("CurrentNHSession");
            // http://www.amazedsaint.com/2010/02/top-5-common-programming-mistakes-net.html case 3: unhooking event handlers appropriately after wiring them
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

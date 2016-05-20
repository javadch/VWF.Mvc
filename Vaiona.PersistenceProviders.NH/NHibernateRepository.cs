using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NHibernate;
using Vaiona.Persistence.Api;
using NHibernate.Engine;
using System.Diagnostics.Contracts;
using System.Collections;

namespace Vaiona.PersistenceProviders.NH
{
    /// <summary>
    /// The methods of the repository, do not push the changes to the underlying database! to do so you need to commit the transaction which is under the control of the unit of work!
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class NHibernateRepository<TEntity> : NHibernateReadonlyRepository<TEntity>, IRepository<TEntity> where TEntity : class
    {
        //public IUnitOfWork UnitOfWork { get { return(this.UoW as IUnitOfWork);}  }

        internal NHibernateRepository(IUnitOfWork uow)
            : base(uow)
        {
        }

        public bool IsTransient(object proxy)
        {
            
            ISessionImplementor isim = null;
            if (UoW is NHibernateUnitOfWork)
                isim = (this.UnitOfWork as NHibernateUnitOfWork).Session.GetSessionImplementation();
            else if (UoW is NHibernateBulkUnitOfWork)
                isim = (this.UnitOfWork as NHibernateBulkUnitOfWork).Session.GetSessionImplementation();
            bool result = NHibernate.Engine.ForeignKeys.IsTransient("", proxy, true, isim);
            return (result);
        }

        //needs more tests
        public TEntity Merge(TEntity entity)
        {
            //session.Lock(entity, LockMode.None);
            //UoW.Session.Merge<TEntity>(entity);
            if (UoW is NHibernateUnitOfWork)
                ((NHibernateUnitOfWork)UoW).Session.Merge<TEntity>(entity);
            return (entity);
        }
 
        /// <summary>
        /// In Stateless Mode, it only INSERTs the entity
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public bool Put(TEntity entity)
        {
            //session.Lock(entity, LockMode.None);
            applyStateInfo(entity);
            applyAuditInfo(entity);
            //UoW.Session.SaveOrUpdate(entity);
            if (UoW is NHibernateUnitOfWork)
            {
                ((NHibernateUnitOfWork)UoW).Session.SaveOrUpdate(entity);
                return true;
            }
            else if (UoW is NHibernateBulkUnitOfWork)
            {   // check to see whether the entity is a new object to be inserted or an existing one to be updated. 
                // the stateless session does not keep track of the entities!
                ((NHibernateBulkUnitOfWork)UoW).Session.Insert(entity);
                return (true);
            }
            return (false);
        }

        public bool Put(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                //session.Lock(entity, LockMode.None);
                //applyStateInfo(entity);
                //applyAuditInfo(entity);
                //UoW.Session.SaveOrUpdate(entity);
                if (!Put(entity))
                    return false;
            }
            return (true);
        }

        public bool Delete(TEntity entity)
        {
            //UoW.Session.Delete(entity);
            //return (true);
            if (UoW is NHibernateUnitOfWork)
            {
                ((NHibernateUnitOfWork)UoW).Session.Delete(entity);
                return true;
            }
            else if (UoW is NHibernateBulkUnitOfWork)
            {
                ((NHibernateBulkUnitOfWork)UoW).Session.Delete(entity);
                return (true);
            }
            return (false);
        }

        public bool Delete(IEnumerable<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                //UoW.Session.Delete(entity);
                if (!Delete(entity))
                    return false;
            }
            return (true);
        }

        /// <summary>
        /// Use this only for delete or update in bulk mode
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="parameters"></param>
        /// <param name="isNativeOrORM"></param>
        /// <returns></returns>
        public int Execute(string queryString, Dictionary<string, object> parameters, bool isNativeOrORM = false)
        {
            if (parameters != null && !Contract.ForAll(parameters, (KeyValuePair<string, object> p) => p.Value != null))
                throw new ArgumentException("The parameter array has a null element", "parameters");

            IQuery query = null;
            if (isNativeOrORM == false) // ORM native query: like HQL
            {
                if (UoW is NHibernateUnitOfWork)
                    query = ((NHibernateUnitOfWork)UoW).Session.CreateQuery(queryString);
                else if (UoW is NHibernateBulkUnitOfWork)
                    query = ((NHibernateBulkUnitOfWork)UoW).Session.CreateQuery(queryString);
            }
            else // Database native query
            {
                //query = UoW.Session.CreateSQLQuery(queryString).AddEntity(typeof(TEntity));
                if (UoW is NHibernateUnitOfWork)
                    query = ((NHibernateUnitOfWork)UoW).Session.CreateSQLQuery(queryString).AddEntity(typeof(TEntity));
                else if (UoW is NHibernateBulkUnitOfWork)
                    query = ((NHibernateBulkUnitOfWork)UoW).Session.CreateSQLQuery(queryString).AddEntity(typeof(TEntity));
            }
            if (parameters != null)
            {
                foreach (var item in parameters)
                {
                    if (item.Value is IList || item.Value is ICollection)
                    {
                        query.SetParameterList(item.Key, (IEnumerable)item.Value);
                    }
                    else
                    {
                        query.SetParameter(item.Key, item.Value);
                    }
                }
            }            
            return (query.ExecuteUpdate());
        }

        private void applyAuditInfo(TEntity entity)
        {
            // check unsaved-value-check to know whether object is new or updated. use this info for state management
            // check whether entity is a BaseEntity, BusinessEntity or something else
            // throw new NotImplementedException();
        }

        private void applyStateInfo(TEntity entity)
        {
            // check unsaved-value-check to know whether object is new or updated. use this info for state management
            // throw new NotImplementedException();
        }
    }
}

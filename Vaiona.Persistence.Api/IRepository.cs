using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;

namespace Vaiona.Persistence.Api
{
    public interface IRepository<TEntity> : IReadOnlyRepository<TEntity> where TEntity : class // BaseEntity
    {
        bool IsTransient(object proxy);

        TEntity Merge(TEntity entity);
        bool Put(TEntity entity);
        bool Put(IEnumerable<TEntity> entities);        
        bool Delete(TEntity entity);
        bool Delete(IEnumerable<TEntity> entities);
        
        IUnitOfWork UnitOfWork { get; }

        int Execute(string queryString, Dictionary<string, object> parameters, bool isNativeOrORM = false);
    }

    //public interface IIntKeyedRepository<TEntity> : IRepository<TEntity> where TEntity : class
    //{
    //    TEntity FindBy(int id);
    //}
}

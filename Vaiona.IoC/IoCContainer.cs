using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vaiona.IoC
{
    public interface IoCContainer
    {
        object Resolve(Type t);
        //IEnumerable<object> ResolveAll(Type t);

        object Resolve<T>();
        //IEnumerable<T> ResolveAll<T>();
        
        void Teardown(object obj);
        
        void StartSessionLevelContainer();
        void ShutdownSessionLevelContainer();
        object ResolveForSession<T>();
        object ResolveForSession(Type t);
    }
}

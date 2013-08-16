using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Vaiona.Persistence.Api;

namespace Vaiona.Web.Mvc.Data
{
    /// <summary>
    /// Using this class is recommended just in MVC applications. in case the usage of SessionPerRequestModule should be avoided by removing/ commenting it in the web.config 
    /// Enabling this class can happen at the global.asax application start method by RegisterGlobalFilters
    /// issue: the pManager object should be kept between OnActionExecuted and OnResultExecuted or OnException
    /// <example>
    /// <code>
    /// public static void RegisterGlobalFilters(GlobalFilterCollection filters) {
    /// filters.Add(new HandleErrorAttribute());
    /// filters.Add(new ProfilerAttribute());
    /// }
    /// </code>
    /// </example>
    /// </summary>
    public class PersistenceContextProviderAttribute : ActionFilterAttribute, IExceptionFilter//, IActionFilter, IResultFilter
    {
        IPersistenceManager pManager = null;

        public PersistenceContextProviderAttribute()
        {
            pManager = PersistenceFactory.GetPersistenceManager();
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext.ActionDescriptor.GetType().GetCustomAttributes(typeof(DoesNotNeedDataAccess), true).Count() > 0)
                return;
            //if (filterContext.IsChildAction)
            //    return;
            pManager.StartConversation();
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.ActionDescriptor.GetType().GetCustomAttributes(typeof(DoesNotNeedDataAccess), true).Count() > 0)
                return;
            pManager.EndConversation();
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
        }

        public void OnException(ExceptionContext filterContext)
        {
            pManager.EndConversation();
            //pManager.EndContext();
        }
    }
}

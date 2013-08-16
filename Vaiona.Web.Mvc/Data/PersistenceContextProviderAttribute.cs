using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Vaiona.Persistence.Api;
using Vaiona.Util.Cfg;

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
            if (filterContext.ActionDescriptor.GetCustomAttributes(typeof(DoesNotNeedDataAccess), true).Count() > 0)
                return;
            //if (filterContext.IsChildAction)
            //    return;

            // conversations are managed per request, which means one conversation is created and torn down per MVC action
            // this mechanism does not support conversation per web session! if that scenario is needed the right way is not move StartConversation and Shutdown/ EndConversation methods
            // to Session_Start and Session_End respectively. The scenario is not tested, though!
            pManager.StartConversation();
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.ActionDescriptor.GetCustomAttributes(typeof(DoesNotNeedDataAccess), true).Count() > 0)
                return;
            if (AppConfiguration.AutoCommitTransactions)
                pManager.EndConversation();
            else
                pManager.ShutdownConversation();                        
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
        }

        public void OnException(ExceptionContext filterContext)
        {
            pManager.ShutdownConversation();
            //pManager.EndContext();
        }
    }
}

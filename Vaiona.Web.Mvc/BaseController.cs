using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Vaiona.Web.Mvc
{
    public abstract class BaseController : Controller
    {
        protected IList<IDisposable> Disposables;

        protected BaseController()
        {
            Disposables = new List<IDisposable>();
        }

        protected override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            foreach (var disposable in Disposables)
            {
                disposable.Dispose();
            }

            base.OnActionExecuted(filterContext);
        }
    }
}

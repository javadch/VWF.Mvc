using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Vaiona.Web.Mvc.Data;
using Vaiona.Entities.Logging;
using Vaiona.Logging;
using Vaiona.Logging.Aspects;
using Vaiona.MultiTenancy.Api;

namespace Vaiona.Web.Mvc.Shell.Test.Controllers
{
    public class HomeController : Controller
    {
        [DoesNotNeedDataAccess]
        public ActionResult Index()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            ITenantRegistrar tenantRegistrar = MultiTenantFactory.GetTenantRegistrar();
            return View();
        }

        [DoesNotNeedDataAccess]
        [RecordCall]
        public ActionResult Trace()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            return View();
        }

        [DoesNotNeedDataAccess]
        [MeasurePerformance]
        public ActionResult Perf()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            return View();
        }

        [DoesNotNeedDataAccess]
        [Diagnose]
        public ActionResult Diag(int id)
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            int x = MonitoredMethod("ABC", true);
            return View("Index");
        }

        [Diagnose]
        private int MonitoredMethod(string text, bool checkIt)
        {
            return (new Random()).Next();
        }

        [DoesNotNeedDataAccess]
        [LogExceptions]
        public ActionResult Ex()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            throw new Exception("I am uncaught exception wanted to be logged. Hurray!");
            return View();
        }

        [DoesNotNeedDataAccess]
        public ActionResult Custom()
        {
            ViewBag.Message = "Welcome to ASP.NET MVC!";
            LoggerFactory.LogCustom("I am a custom message, please log me!");
            return View();
        }

        [DoesNotNeedDataAccess]
        public ActionResult About()
        {
            return View();
        }
    }
}

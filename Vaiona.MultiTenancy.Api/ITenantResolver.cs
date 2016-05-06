using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Vaiona.Model.MTnt;

namespace Vaiona.MultiTenancy.Api
{
    public interface ITenantResolver
    {
        Tenant Resolve(HttpRequest request);
        Tenant Resolve(string request);
        Tenant Resolve(Uri request);
    }
}

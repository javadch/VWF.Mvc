using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Vaiona.Model.MTnt;
using Vaiona.MultiTenancy.Api;

namespace Vaiona.MultiTenancy.Services
{
    public class TenantResolver : ITenantResolver
    {
        public Tenant Resolve(Uri request)
        {
            throw new NotImplementedException();
        }

        public Tenant Resolve(string request)
        {
            throw new NotImplementedException();
        }

        public Tenant Resolve(HttpRequest request)
        {
            throw new NotImplementedException();
        }
    }
}

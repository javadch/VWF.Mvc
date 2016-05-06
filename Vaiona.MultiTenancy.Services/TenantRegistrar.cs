using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaiona.Model.MTnt;
using Vaiona.MultiTenancy.Api;

namespace Vaiona.MultiTenancy.Services
{
    public class TenantRegistrar : ITenantRegistrar
    {
        public void Activate(string id)
        {
            throw new NotImplementedException();
        }

        public List<Tenant> GetAll()
        {
            throw new NotImplementedException();
        }

        public void Inactivate(string id)
        {
            throw new NotImplementedException();
        }

        public void Register(object tenant)
        {
            throw new NotImplementedException();
        }

        public void Unregister(string id)
        {
            throw new NotImplementedException();
        }
    }
}

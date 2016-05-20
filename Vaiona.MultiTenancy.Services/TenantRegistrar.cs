using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaiona.Model.MTnt;
using Vaiona.MultiTenancy.Api;
using Vaiona.Utils.Cfg;

namespace Vaiona.MultiTenancy.Services
{
    public class TenantRegistrar : ITenantRegistrar
    {
        XmlTenantStore store = new XmlTenantStore(null); // think about the path provider
        //public List<Tenant> GetAll()
        //{
        //    List<Tenant> tenants = store.List();
        //    tenants.ForEach(p => tenants.Add(store.Load(p.Id)));
        //    return tenants;
        //}

        public void Activate(string id)
        {
            Tenant tenant = null;
            try
            {
                tenant = store.Tenants.Where(p => id.Equals(p.Id, StringComparison.InvariantCultureIgnoreCase)).Single();
                tenant.Status = TenantStatus.Active;
            }
            catch
            {
                throw new Exception(string.Format("Tenant with id '{0}' was not found! Operation aborted.", id));
            }
            store.Update(tenant);
        }

        public void Inactivate(string id)
        {
            Tenant tenant = null;
            try
            {
                tenant = store.Tenants.Where(p => id.Equals(p.Id, StringComparison.InvariantCultureIgnoreCase)).Single();
                tenant.Status = TenantStatus.Inactive;
            }
            catch
            {
                throw new Exception(string.Format("Tenant with id '{0}' was not found! Operation aborted.", id));
            }
            store.Update(tenant);
        }

        public void Register(Tenant tenant)
        {
            // create the tenant entry
            // check the tenant folder is created
            // check the file based properties have thier own files in the tenent folder
            // check/create the tenant's manifest
            store.Create(tenant);
        }

        public void Unregister(string id)
        {
            Tenant tenant = null;
            try
            {
                tenant = store.Tenants.Where(p => id.Equals(p.Id, StringComparison.InvariantCultureIgnoreCase)).Single();
                tenant.Status = TenantStatus.Active;
            }
            catch
            {
                throw new Exception(string.Format("Tenant with id '{0}' was not found! Operation aborted.", id));
            }
            store.Remove(tenant);
        }
    }
}

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
                throw new Exception(string.Format("Tenant '{0}' was not found. Operation aborted.", id));
            }
            store.UpdateStatus(tenant);
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
                throw new Exception(string.Format("Tenant '{0}' was not found. Operation aborted.", id));
            }
            store.UpdateStatus(tenant);
        }

        public void MakeDefault(string id)
        {
            Tenant tenant = null;
            try
            {
                // It is not reaaly needed to set the default here, but it is done so that if a request arrives during the update
                // it is served with the latest information
                store.Tenants.ForEach(p => p.IsDefault = false);
                tenant = store.Tenants.Where(p => id.Equals(p.Id, StringComparison.InvariantCultureIgnoreCase)).Single();
                tenant.IsDefault = true;
            }
            catch
            {
                throw new Exception(string.Format("Tenant '{0}' was not found. Operation aborted.", id));
            }
            store.MakeDefault(tenant);
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
            if (store.Tenants.Count() <= 1)
                throw new Exception(string.Format("Tenant '{0}' could not be unregistered. It is the only registered tenent.", id));
            Tenant tenant;
            try
            {
                tenant = store.Tenants.Where(p => id.Equals(p.Id, StringComparison.InvariantCultureIgnoreCase)).Single();
            }
            catch
            {
                throw new Exception(string.Format("Tenant '{0}' was not found! Operation aborted.", id));
            }

            if (tenant.IsDefault == true)
                throw new Exception(string.Format("Tenant '{0}' could not be unregistered. It is the default tenent.", id));
            try
            {
                store.Remove(tenant);
            }
            catch
            {
                throw new Exception(string.Format("Tenant '{0}' was not unresigtered! Operation aborted.", id));
            }
        }
    }
}

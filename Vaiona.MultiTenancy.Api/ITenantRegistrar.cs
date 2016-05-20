using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vaiona.Model.MTnt;

namespace Vaiona.MultiTenancy.Api
{
    public interface ITenantRegistrar
    {
        /// <summary>
        /// It is supposed thw tenant object to be the full path to a zip file containing all the required information to register a tenant.
        /// Update the tenant store and the loaded information
        /// </summary>
        /// <param name="tenant"></param>
        void Register(Tenant tenant);
        void Unregister(string id);
        void Activate(string id);
        void Inactivate(string id);
        //List<Tenant> GetAll();
    }
}

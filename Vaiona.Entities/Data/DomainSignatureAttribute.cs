using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Vaiona.Entities.Data
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class DomainSignatureAttribute : Attribute { }
}

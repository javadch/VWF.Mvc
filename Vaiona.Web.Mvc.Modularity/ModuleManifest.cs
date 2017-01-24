using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Vaiona.Web.Mvc.Modularity
{
    public class ModuleManifest
    {
        private XElement xManifest; //maybe not needed
        public ModuleManifest(XElement manifestElement)
        {
            xManifest = manifestElement;
            // poplulate the manifest property using the manifest and catalog information
            Name = xManifest.Attribute("moduleId").Value;
            Version = xManifest.Attribute("version").Value;
            Description = xManifest.Element("Description").Value;
        }

        public string Version { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
    }
}

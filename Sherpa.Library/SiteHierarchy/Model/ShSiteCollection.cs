using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShSiteCollection
    {
        public List<ShFeature> SiteFeatures { get; set; }
        public ShWeb RootWeb { get; set; }

        /* The references to the configuration files within the config folder */
        public string[] FieldConfigurations { get; set; }
        public string[] ContentTypeConfigurations { get; set; }
        public string[] TaxonomyConfigurations { get; set; }
        public string[] SandboxedSolutions { get; set; }

        public ShSiteCollection()
        {
            SiteFeatures = new List<ShFeature>();
        }
    }
}

using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShSiteCollection
    {
        public List<ShFeature> SiteFeatures { get; set; }
        public ShWeb RootWeb { get; set; }

        public string[] SandboxedSolutions { get; set; }
        public string[] FieldConfigurations { get; set; }
        public string[] ContentTypeConfigurations { get; set; }
        public string[] TaxonomyConfigurations { get; set; }
        public string[] SearchConfigurations { get; set; }

        public ShSiteCollection()
        {
            SiteFeatures = new List<ShFeature>();
        }
    }
}

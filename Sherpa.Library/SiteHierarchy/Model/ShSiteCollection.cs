using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShSiteCollection
    {
        public List<ShPermissionLevel> PermissionLevels { get; set; }
        public List<ShFeature> SiteFeatures { get; set; }
        public List<ShCustomAction> CustomActions { get; set; }
        public ShWeb RootWeb { get; set; }

        public string CustomActionsPrefix { get; set; }
        public string[] SandboxedSolutions { get; set; }
        public string[] FieldConfigurations { get; set; }
        public string[] ContentTypeConfigurations { get; set; }
        public string[] TaxonomyConfigurations { get; set; }
        public string[] SearchConfigurations { get; set; }

        public ShSiteCollection()
        {
            SiteFeatures = new List<ShFeature>();
            PermissionLevels = new List<ShPermissionLevel>();
            CustomActions = new List<ShCustomAction>();
        }
    }
}

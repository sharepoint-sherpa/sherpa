using System;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShFeature
    {
        public Guid FeatureId { get; set; }
        public string FeatureName { get; set; }
        public bool ReactivateAlways { get; set; }
        public bool ContentTypeDependency { get; set; }
    }
}

using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShWebPartReference
    {
        public string FileName { get; set; }
        public string ZoneID { get; set; }
        public int Order { get; set; }
        public Dictionary<string, string> PropertiesOverrides { get; set; }
    }
}

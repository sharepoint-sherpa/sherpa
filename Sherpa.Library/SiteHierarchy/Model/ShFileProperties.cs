using System.Collections.Generic;
using Microsoft.SharePoint.Client;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShFileProperties
    {
        public string Path { get; set; }
        public string Url { get; set; }
        public bool ReplaceWebParts { get; set; }

        /* Published, Draft, CheckOut (not supported) */
        public FileLevel Level { get; set; }
        public Dictionary<string, string> Properties { get; set; }
        public List<ShWebPartReference> WebParts { get; set; }

        public ShFileProperties()
        {
            Properties = new Dictionary<string, string>();
            WebParts = new List<ShWebPartReference>();
            Level = FileLevel.Published;
        }
    }
}

using System.Collections.Generic;
using Microsoft.SharePoint.Client;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShFileProperties
    {
        public string Path { get; set; }
        public string Url { get; set; }
        /* Published, Draft, CheckOut */
        public FileLevel Level { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public ShFileProperties()
        {
            Level = FileLevel.Published;
        }
    }
}

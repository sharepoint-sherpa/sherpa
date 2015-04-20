using System.Collections.Generic;
using System.Globalization;
using Microsoft.SharePoint.Client;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShFileProperties
    {
        public string Path { get; set; }
        public string Url { get; set; }
        public Dictionary<string, string> Properties { get; set; }
    }
}

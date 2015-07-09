using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Installer.Model
{
    public class ShEnvironment
    {
        public string Name { get; set; }
        public string UrlToSite { get; set; }
        public string UserName { get; set; }
        public string RootPath { get; set; }
        public string SiteHierarchy { get; set; }
        public bool SharePointOnline { get; set; }
    }
}

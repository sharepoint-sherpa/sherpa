using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShComposedLook
    {
        public string Title { get; set; }
        public string Name { get; set; }
        public string ThemeUrl { get; set; }
        public string FontSchemeUrl { get; set; }
        public string ImageUrl { get; set; }
        public string MasterPageUrl { get; set; }
        public int DisplayOrder { get; set; }
    }
}

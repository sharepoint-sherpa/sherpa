using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShView
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public List<string> ViewFields { get; set; }
        public string Query { get; set; }
        public int RowLimit { get; set; }

        public ShView()
        {
            ViewFields = new List<string>();
        }
    }
}

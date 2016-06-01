using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShListData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public ShListDataRoot Data { get; set; }

        public ShListData()
        {
            Data = new ShListDataRoot();
        }
    }

    public class ShListDataRoot
    {
        public List<IShListDataItem> Rows { get; set; }
        public ShListDataRoot()
        {
            Rows = new List<IShListDataItem>();
        }
    }
}

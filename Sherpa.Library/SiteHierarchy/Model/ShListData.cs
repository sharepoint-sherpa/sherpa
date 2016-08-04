using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShListData : IShListData
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public ShListDataRoot Data { get; set; }

        public ShListData()
        {
            Data = new ShListDataRoot();
        }
    }

    public class ShListDataRoot : IShListDataRoot
    {
        public List<ShListDataItem> Rows { get; set; }
        public ShListDataRoot()
        {
            Rows = new List<ShListDataItem>();
        }
    }
}

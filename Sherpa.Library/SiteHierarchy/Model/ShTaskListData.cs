using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShTaskListData : ShListData
    {
        public new ShTaskListDataRoot Data { get; set; }
        public ShTaskListData()
        {
            Data = new ShTaskListDataRoot();
        }
    }

    public class ShTaskListDataRoot : IShListDataRoot
    {
        public List<ShTaskListDataItem> Rows { get; set; }
        public ShTaskListDataRoot()
        {
            Rows = new List<ShTaskListDataItem>();
        }
    }
}

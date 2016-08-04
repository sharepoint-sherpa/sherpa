using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShTaskListDataItem : ShListDataItem
    {
        [JsonIgnore]
        public int ParentID { get; set; }

        [JsonIgnore]
        public double Order { get; set; }
        [JsonProperty(Order = 1)]
        public List<ShTaskListDataItem> Rows;
        public ShTaskListDataItem() : base(0)
        {
            Rows = new List<ShTaskListDataItem>();
            ParentID = 0;
        }
        public ShTaskListDataItem(int id) : base(id)
        {
            Rows = new List<ShTaskListDataItem>();
            ParentID = 0;
        }
        public ShTaskListDataItem(int id, int parentId) : base(id)
        {
            ParentID = parentId;
            Rows = new List<ShTaskListDataItem>();
        }
        public bool ShouldSerializeRows()
        {
            return Rows.Count > 0;
        }
    }
}

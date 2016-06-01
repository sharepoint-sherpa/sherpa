using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShTaskListItemData : ShListDataItem
    {
        [JsonIgnore]
        public int ParentID { get; set; }

        [JsonIgnore]
        public double Order { get; set; }
        [JsonProperty(Order = 1)]
        public List<ShTaskListItemData> Rows;
        public ShTaskListItemData(int id) : base(id)
        {
            Fields = new List<ShFieldValue>();
            Rows = new List<ShTaskListItemData>();
            ParentID = 0;
        }
        public ShTaskListItemData(int id, int parentId) : base(id)
        {
            ParentID = parentId;
            Fields = new List<ShFieldValue>();
            Rows = new List<ShTaskListItemData>();
        }
        public bool ShouldSerializeRows()
        {
            return Rows.Count > 0;
        }
    }
}

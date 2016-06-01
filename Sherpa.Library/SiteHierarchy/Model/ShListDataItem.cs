using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShListDataItem : IShListDataItem
    {
        [JsonIgnore]
        public int ID { get; set; }
        [JsonProperty(Order = 0)]
        public List<ShFieldValue> Fields;

        public ShListDataItem(int id)
        {
            ID = id;
            Fields = new List<ShFieldValue>();
        }
    }
}

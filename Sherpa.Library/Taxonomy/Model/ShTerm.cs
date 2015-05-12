using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTerm : ShTermSetItem
    {
        [JsonProperty(Order = 7)]
        public IDictionary<string, string> LocalCustomProperties { get; set; }
        [JsonProperty(Order = 8)]
        public List<ShTermLabel> Labels { get; set; }

        public ShTerm()
        {
            LocalCustomProperties = new Dictionary<string, string>();
            Labels = new List<ShTermLabel>();
        }

        public ShTerm(Guid id, string title) : base(id, title)
        {
            LocalCustomProperties = new Dictionary<string, string>();
            Labels = new List<ShTermLabel>();
        }

        /// <summary>
        /// Tells Newtonsoft Json.NET not to serialize based on condition
        /// </summary>
        /// <returns>true if it should be serialized, false if not</returns>
        public bool ShouldSerializeLocalCustomProperties()
        {
            return LocalCustomProperties.Count > 0;
        }
    }
}

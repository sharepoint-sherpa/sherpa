using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTermGroup : ShTaxonomyItem
    {
        [JsonProperty(Order = 3)]
        public List<ShTermSet> TermSets { get; set; }
        public ShTermGroup()
        {
            TermSets = new List<ShTermSet>();
        }
        public ShTermGroup(Guid id, string title) : base(id, title)
        {
            TermSets = new List<ShTermSet>();
        }
    }
}

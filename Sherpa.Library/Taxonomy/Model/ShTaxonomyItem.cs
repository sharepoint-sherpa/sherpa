using System;
using Newtonsoft.Json;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTaxonomyItem
    {
        [JsonProperty(Order = 1)]
        public Guid Id { get; set; }
        [JsonProperty(Order = 2)]
        public string Title { get; set; }

        public ShTaxonomyItem() {}
        public ShTaxonomyItem(Guid id, string title)
        {
            Title = title;
            Id = id;
        }
    }
}

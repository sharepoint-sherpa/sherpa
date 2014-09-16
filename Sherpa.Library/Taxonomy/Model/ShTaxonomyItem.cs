using System;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTaxonomyItem
    {
        public string Title { get; set; }
        public Guid Id { get; set; }

        public ShTaxonomyItem() {}
        public ShTaxonomyItem(Guid id, string title)
        {
            Title = title;
            Id = id;
        }
    }
}

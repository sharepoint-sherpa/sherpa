using System;
using System.Collections.Generic;

namespace Sherpa.Library.Taxonomy.Model
{
    public abstract class ShTermSetItem : ShTaxonomyItem
    {
        public string CustomSortOrder { get; set; }
        public bool NotAvailableForTagging { get; set; }
        public List<ShTerm> Terms { get; set; }

        protected ShTermSetItem()
        {
            Terms = new List<ShTerm>();
        }
        protected ShTermSetItem(Guid id, string title) : base(id, title)
        {
            Terms = new List<ShTerm>();
        }
        public bool ShouldSerializeTerms()
        {
            return Terms.Count > 0;
        }
    }
}

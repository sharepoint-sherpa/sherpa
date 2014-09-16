using System;
using System.Collections.Generic;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTermGroup : ShTaxonomyItem
    {
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

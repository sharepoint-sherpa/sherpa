using System;
using System.Collections.Generic;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTermSetGroup : ShTermItemBase
    {
        public List<ShTermSet> TermSets { get; set; }
        public ShTermSetGroup()
        {
            TermSets = new List<ShTermSet>();
        }
        public ShTermSetGroup(Guid id, string title) : base(id, title)
        {
            TermSets = new List<ShTermSet>();
        }
    }
}

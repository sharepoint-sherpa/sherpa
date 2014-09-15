using System;
using System.Collections.Generic;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTermSet : ShTermItemBase
    {
        public List<ShTerm> Terms { get; set; }

        public ShTermSet()
        {
            Terms = new List<ShTerm>();
        }
        public ShTermSet(Guid id, string title) : base(id, title)
        {
            Terms = new List<ShTerm>();
        }
    }
}

using System;
using System.Collections.Generic;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTermGroup
    {
        public string Title { get; set; }
        public Guid Id { get; set; }
        public List<ShTermSet> TermSets { get; set; }
        public ShTermGroup()
        {
            TermSets = new List<ShTermSet>();
        }
        public ShTermGroup(Guid id, string title)
        {
            Title = title;
            Id = id;
            TermSets = new List<ShTermSet>();
        }
    }
}

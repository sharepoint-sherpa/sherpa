using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sherpa.Library.Taxonomy.Model
{
    public abstract class ShTermItemBase
    {
        public string Title { get; set; }
        public Guid Id { get; set; }
        public string CustomSortOrder { get; set; }
        public bool NotAvailableForTagging { get; set; }
        public List<ShTerm> Terms { get; set; }

        protected ShTermItemBase()
        {
            Terms = new List<ShTerm>();
        }
        protected ShTermItemBase(Guid id, string title)
        {
            Title = title;
            Id = id;
            Terms = new List<ShTerm>();
        }
        public bool ShouldSerializeTerms()
        {
            return Terms.Count > 0;
        }
    }
}

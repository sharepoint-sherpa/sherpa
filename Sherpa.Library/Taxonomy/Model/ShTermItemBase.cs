using System;

namespace Sherpa.Library.Taxonomy.Model
{
    public abstract class ShTermItemBase
    {
        public string Title { get; set; }
        public Guid Id { get; set; }
        public string CustomSortOrder { get; set; }

        protected ShTermItemBase() { }
        protected ShTermItemBase(Guid id, string title)
        {
            Title = title;
            Id = id;
        }
    }
}

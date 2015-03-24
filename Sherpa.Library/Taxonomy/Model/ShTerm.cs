using System;
using System.Collections.Generic;

namespace Sherpa.Library.Taxonomy.Model
{
    public class ShTerm : ShTermSetItem
    {
        public Dictionary<string, string> LocalCustomProperties { get; set; }
        public ShTerm(){
            LocalCustomProperties = new Dictionary<string, string>();
        }
        public ShTerm(Guid id, string title) : base(id, title){
            LocalCustomProperties = new Dictionary<string, string>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShFieldValue
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public ShFieldValue(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}

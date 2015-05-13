﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShGroup
    {
        public string Name { get; set; }
        public ShAssociatedGroup AssociatedGroup { get; set; }

        public ShGroup()
        {
            Name = "";
            AssociatedGroup = null;
        }
    }
}
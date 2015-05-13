using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShRoleAssignment
    {
        public ShGroup Group { get; set; }
        public string PermissionLevel { get; set; }

        public ShRoleAssignment()
        {

        }
    }
}

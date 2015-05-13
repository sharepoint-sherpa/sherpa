using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShPermissionScheme
    {
        public bool BreakInheritance { get; set; }
        public bool RemoveDefaultRoleAssignments { get; set; }
        public List<ShRoleAssignment> RoleAssignments { get; set; }

        public ShPermissionScheme()
        {
            RoleAssignments = new List<ShRoleAssignment>();
        }
    }
}
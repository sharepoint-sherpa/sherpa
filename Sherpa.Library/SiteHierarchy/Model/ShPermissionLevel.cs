using Microsoft.SharePoint.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShPermissionLevel
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public List<PermissionKind> BasePermissions { get; set; }

        public ShPermissionLevel()
        {
            BasePermissions = new List<PermissionKind>();
        }
    }
}

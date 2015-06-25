using System.Linq;
using System.Reflection;
using Microsoft.SharePoint.Client;
using log4net;
using Sherpa.Library.SiteHierarchy.Model;
using System.Collections.Generic;


namespace Sherpa.Library.SiteHierarchy
{
    public class PermissionManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void SetUpCustomPermissionLevels(ClientContext context, List<ShPermissionLevel> permissionLevels)
        {
            foreach (var permissionLevel in permissionLevels)
            {
                context.Load(context.Site.RootWeb.RoleDefinitions);
                context.ExecuteQuery();

                var existingPermissionLevel = context.Site.RootWeb.RoleDefinitions.FirstOrDefault(x => x.Name.Equals(permissionLevel.Name));
                if (existingPermissionLevel == null)
                {
                    Log.Info("Creating permission level " + permissionLevel.Name);
                    BasePermissions permissions = new BasePermissions();
                    foreach (var basePermission in permissionLevel.BasePermissions)
                    {
                        permissions.Set(basePermission);
                    }
                    RoleDefinitionCreationInformation roleDefinitionCreationInfo = new RoleDefinitionCreationInformation();
                    roleDefinitionCreationInfo.BasePermissions = permissions;
                    roleDefinitionCreationInfo.Name = permissionLevel.Name;
                    roleDefinitionCreationInfo.Description = permissionLevel.Description;
                    RoleDefinition roleDefinition = context.Site.RootWeb.RoleDefinitions.Add(roleDefinitionCreationInfo);
                    context.ExecuteQuery();
                }
            }
        }
    }
}

using System;
using System.Reflection;
using Microsoft.SharePoint.Client;
using log4net;
using Sherpa.Library.SiteHierarchy.Model;
using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy
{
    public class CustomActionsManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private string CreateUserActioName(ShCustomAction customAction)
        {
            if (!string.IsNullOrEmpty(customAction.Id))
            {
                return customAction.Id;
            }
            else if (!String.IsNullOrEmpty(customAction.ScriptSrc))
            {
                return customAction.ScriptSrc.Split('/')[customAction.ScriptSrc.Split('/').Length - 1].Replace(".", "");
            }
            else if (!String.IsNullOrEmpty(customAction.Sequence.ToString()))
            {
                return customAction.Sequence.ToString();
            }
            return "";
        }

        public void SetUpCustomActions(ClientContext context, string CustomActionsPrefix, List<ShCustomAction> customActions)
        {
            if (customActions.Count > 0)
            {
                if (CustomActionsPrefix == null || CustomActionsPrefix.Equals(String.Empty))
                {
                    Log.Info("You need to set the property 'CustomActionsPrefix' which will be used for the Custom Action Name.");
                    return;
                }

                Log.Info("Adding custom actions");
                Site site = context.Site;
                context.Load(site);
                context.Load(site.UserCustomActions);
                context.ExecuteQuery();

                for (var i = site.UserCustomActions.Count - 1; i >= 0; i--)
                {
                    var customAction = site.UserCustomActions[i];
                    if (customAction.Name.StartsWith(CustomActionsPrefix))
                    {
                        customAction.DeleteObject();
                    }
                }

                if (context.HasPendingRequest)
                {
                    context.ExecuteQuery();
                }

                foreach (ShCustomAction customAction in customActions)
                {
                    if (customAction.Location == null)
                    {
                        Log.Error("You need to specify a location for your Custom Action. Ignoring " + customAction.ScriptSrc);
                        continue;
                    }

                    Log.DebugFormat("Adding custom action with src '{0}' at location '{1}'", customAction.ScriptSrc, customAction.Location);

                    UserCustomAction userCustomAction = site.UserCustomActions.Add();
                    userCustomAction.Location = customAction.Location;
                    userCustomAction.Sequence = customAction.Sequence;
                    userCustomAction.ScriptSrc = customAction.ScriptSrc;
                    userCustomAction.ScriptBlock = customAction.ScriptBlock;
                    userCustomAction.Name = String.Format("{0}_{1}", CustomActionsPrefix, CreateUserActioName(customAction));
                    userCustomAction.Description = customAction.Description;
                    userCustomAction.RegistrationType = customAction.RegistrationType;
                    userCustomAction.Title = customAction.Title;
                    userCustomAction.Url = customAction.Url;
                    userCustomAction.ImageUrl = customAction.ImageUrl;
                    userCustomAction.Group = customAction.Group;

                    try
                    {
                        userCustomAction.Update();
                        context.ExecuteQuery();
                        Log.Debug("Custom action successfully added.");
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.Message);
                    }
                }
            }
        }
    }
}
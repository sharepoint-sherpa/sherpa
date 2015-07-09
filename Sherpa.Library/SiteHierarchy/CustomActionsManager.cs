﻿using System;
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

        public void SetUpCustomActions(ClientContext context, string CustomActionsPrefix, List<ShCustomAction> customActions)
        {
            Log.Info("Adding custom actions");
            Site site = context.Site;
            context.Load(site);
            context.Load(site.UserCustomActions);
            context.ExecuteQuery();

            for (var i = site.UserCustomActions.Count - 1; i >= 0; i--)
            {
                var customAction = site.UserCustomActions[i];
                if (!CustomActionsPrefix.Equals(String.Empty) && !customAction.Name.StartsWith(CustomActionsPrefix))
                {
                    Log.InfoFormat("Keeping custom action with src '{0}' at location '{1}'", site.UserCustomActions[i].ScriptSrc, site.UserCustomActions[i].Location);
                    continue;
                }
                Log.InfoFormat("Removing custom action with src '{0}' at location '{1}'", site.UserCustomActions[i].ScriptSrc, site.UserCustomActions[i].Location);
                customAction.DeleteObject();
                
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

                Log.InfoFormat("Adding custom action with src '{0}' at location '{1}'", customAction.ScriptSrc, customAction.Location);

                UserCustomAction userCustomAction = site.UserCustomActions.Add();
                userCustomAction.Location = customAction.Location;
                userCustomAction.Sequence = customAction.Sequence;
                userCustomAction.ScriptSrc = customAction.ScriptSrc;
                userCustomAction.ScriptBlock = customAction.ScriptBlock;
                userCustomAction.Name = (!CustomActionsPrefix.Equals(String.Empty) ? (CustomActionsPrefix + "_") : "") + customAction.ScriptSrc.Split('/')[customAction.ScriptSrc.Split('/').Length - 1].Replace(".", "");
                userCustomAction.Description = customAction.Description;
                userCustomAction.RegistrationType = customAction.RegistrationType;
                userCustomAction.Title = customAction.Title;
                userCustomAction.ImageUrl = customAction.ImageUrl;
                userCustomAction.Group = customAction.Group;
                
                try
                {
                    userCustomAction.Update();
                    context.ExecuteQuery();
                    Log.Info("Custom action successfully added.");
                }
                catch (Exception e)
                {
                    Log.Error(e.Message);
                }
            }
        }
    }
}
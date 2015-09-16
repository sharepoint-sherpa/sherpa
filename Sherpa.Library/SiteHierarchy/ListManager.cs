using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Sherpa.Library.ContentTypes.Model;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.SiteHierarchy
{
    public class ListManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public void CreateLists(ClientContext context, Web web, List<ShList> listConfig)
        {
            foreach (ShList list in listConfig)
            {
                SetupList(context, web, list);
            }
        }
        
        public void SetupList(ClientContext context, Web web, ShList listConfig)
        {
            var listCollection = web.Lists;
            context.Load(listCollection);
            context.ExecuteQuery();

            var setupList = listCollection.FirstOrDefault(l => l.Title == listConfig.Title);
            if (setupList == null)
            {
                var listCreationInfo = GetListCreationInfoFromConfig(listConfig);
                setupList = listCollection.Add(listCreationInfo);
                context.ExecuteQuery();
            }
            if (listConfig.Hidden.HasValue) setupList.Hidden = listConfig.Hidden.Value;
            if (listConfig.OnQuickLaunch.HasValue) setupList.OnQuickLaunch = listConfig.OnQuickLaunch.Value;
            if (listConfig.VersioningEnabled.HasValue) setupList.EnableVersioning = listConfig.VersioningEnabled.Value;
            setupList.Update();

            SetupFieldsOfList(context, setupList, listConfig);
            SetupContentTypesOfList(context, setupList, listConfig);
            SetupPermissionSchemeOfList(context, setupList, listConfig);
            SetupViewsOfList(context, setupList, listConfig);
        }

        private void SetupFieldsOfList(ClientContext context, List setupList, ShList listConfig)
        {
            foreach (string fieldName in listConfig.Fields)
            {
                if (!setupList.FieldExistsByName(fieldName))
                {
                    Log.DebugFormat("Adding field {0} to list {1}", fieldName, listConfig.Title);
                    var field = context.Site.RootWeb.Fields.GetByInternalNameOrTitle(fieldName);
                    setupList.Fields.Add(field);
                }
                else
                {
                    Log.DebugFormat("Field {0} was not added to list {1} because it already exists", fieldName, listConfig.Title);
                }
            }
            setupList.Update();
            context.Load(setupList);
            context.ExecuteQuery();
        }

        private void SetupViewsOfList(ClientContext context, List list, ShList listConfig)
        {
            context.Load(list.Views);
            context.ExecuteQuery();
            foreach (ShView view in listConfig.Views)
            {
                SetupView(context, list, view);
            }
        }

        private void SetupPermissionSchemeOfList(ClientContext context, List list, ShList listConfig)
        {
            if (listConfig.PermissionScheme != null)
            {
                if (listConfig.PermissionScheme.BreakInheritance)
                {
                    list.BreakRoleInheritance(true, false);
                    list.Update();
                    context.ExecuteQuery();
                }
                if (listConfig.PermissionScheme.RemoveDefaultRoleAssignments)
                {
                    context.Load(list.RoleAssignments);
                    context.ExecuteQuery();
                    for (var i = list.RoleAssignments.Count - 1; i >= 0; i--)
                    {
                        list.RoleAssignments[i].DeleteObject();
                    }
                }
                foreach (var roleAssignment in listConfig.PermissionScheme.RoleAssignments)
                {
                    Group group = null;
                    if (roleAssignment.Group.Name != "")
                    {
                        group = context.Web.SiteGroups.GetByName(roleAssignment.Group.Name);
                    }
                    else
                    {
                        group = GetAssociatedGroup(context, roleAssignment.Group.AssociatedGroup);
                    }

                    RoleDefinitionBindingCollection roleDefBinding = new RoleDefinitionBindingCollection(context);
                    RoleDefinition roleDef = context.Web.RoleDefinitions.GetByName(roleAssignment.PermissionLevel);
                    roleDefBinding.Add(roleDef);
                    list.RoleAssignments.Add(group, roleDefBinding);
                    context.Load(group);
                    context.Load(roleDef);
                    context.ExecuteQuery();
                }
            }
        }

        private Group GetAssociatedGroup(ClientContext context, ShAssociatedGroup assGroup)
        {
            switch (assGroup.Web)
            {
                case "Current":
                    {
                        switch (assGroup.Type)
                        {
                            case "Visitors":
                                {
                                    return context.Web.AssociatedVisitorGroup;
                                }
                            case "Members":
                                {
                                    return context.Web.AssociatedMemberGroup;
                                }
                            case "Owners":
                                {
                                    return context.Web.AssociatedOwnerGroup;
                                }
                        }
                    }
                    break;
                case "Root":
                    {
                        switch (assGroup.Type)
                        {
                            case "Visitors":
                                {
                                    return context.Site.RootWeb.AssociatedVisitorGroup;
                                }
                            case "Members":
                                {
                                    return context.Site.RootWeb.AssociatedMemberGroup;
                                }
                            case "Owners":
                                {
                                    return context.Site.RootWeb.AssociatedOwnerGroup;
                                }
                        }
                    }
                    break;
            }
            return null;
        }


        private void SetupContentTypesOfList(ClientContext context, List list, ShList listConfig)
        {
            if (listConfig.ContentTypes.Count > 0)
            {
                Log.Debug("Starting to configure content types for list " + listConfig.Title);
                var rootWeb = context.Site.RootWeb;
                var rootWebContentTypes = rootWeb.ContentTypes;
                var listContentTypes = list.ContentTypes;
                context.Load(list.RootFolder);
                context.Load(rootWebContentTypes);
                context.Load(listContentTypes);
                context.LoadQuery(rootWebContentTypes.Include(ct => ct.Name));
                context.LoadQuery(listContentTypes.Include(ct => ct.Name));

                list.ContentTypesEnabled = true;
                list.Update();
                context.ExecuteQuery();

                var contentTypesToAdd = new List<ContentType>();
                foreach (var configContentType in listConfig.ContentTypes)
                {
                    Log.Debug("Attempting to add content type " + configContentType);
                    if (listContentTypes.FirstOrDefault(ct => ct.Name == configContentType) == null)
                    {
                        var rootContenttype = rootWebContentTypes.FirstOrDefault(ct => ct.Name == configContentType);
                        if (rootContenttype != null)
                        {
                           // listContentTypes.AddExistingContentType(rootContenttype);
                            contentTypesToAdd.Add(rootContenttype);
                        }
                    }
                }

                foreach (ContentType contentType in contentTypesToAdd)
                {
                    listContentTypes.AddExistingContentType(contentType);
                }
                
                context.Load(listContentTypes);
                context.LoadQuery( listContentTypes.Include(ct => ct.Name) );
                context.ExecuteQuery();

                if (!listConfig.RemoveExisitingContentTypes) return;

                var contentTypesToRemove = new List<ContentType>();
                foreach (ContentType listContentType in listContentTypes)
                {
                    if (!listConfig.ContentTypes.Contains(listContentType.Name))
                    {
                        contentTypesToRemove.Add(listContentType);
                    }
                }
                //Need to do two iterations to avoid deleting from collection that is being iterated
                for (int i = 0; i < contentTypesToRemove.Count; i++)
                {
                    Log.Debug("Attempting to delete content type " + contentTypesToRemove[i].Name);
                    contentTypesToRemove[i].DeleteObject();
                }
                try
                {
                    context.ExecuteQuery();
                }
                catch (Exception)
                {
                    Log.Info("Could not delete ContentTypes from list "+list.RootFolder.ServerRelativeUrl);
                }
                
            }
        }

        private void SetupView(ClientContext context, List list, ShView view)
        {
            var viewCollection = list.Views;
            View setupView = null;
            if (!string.IsNullOrEmpty(view.Title))
            {
                setupView = viewCollection.FirstOrDefault(v => v.Title == view.Title);
                if (setupView == null)
                {
                    setupView = viewCollection.Add(GetViewCreationInfoFromConfig(view));
                    context.ExecuteQuery();
                }
            }
            else if (!string.IsNullOrEmpty(view.Url))
            {
                context.Load(list, x=>x.ParentWebUrl);
                context.Load(list, x => x.ParentWeb);
                context.ExecuteQuery();

                var serverRelativeUrl = UriUtilities.CombineServerRelativeUri(list.ParentWebUrl, view.Url);
                list.ParentWeb.CheckOutFile(serverRelativeUrl);
                setupView = viewCollection.FirstOrDefault(v => v.ServerRelativeUrl == serverRelativeUrl);
            }
            if (setupView != null)
            {
                if (view.ViewFields.Length > 0)
                {
                    setupView.ViewFields.RemoveAll();
                    foreach (var field in view.ViewFields)
                    {
                        setupView.ViewFields.Add(field);
                    }
                }
                setupView.JSLink = view.JSLink;
                setupView.ViewQuery = view.Query;
                setupView.RowLimit = view.RowLimit;
                
                setupView.Update();
                if (!string.IsNullOrEmpty(view.Url))
                {
                    list.ParentWeb.CheckInFile(setupView.ServerRelativeUrl, CheckinType.MajorCheckIn, "updated by sherpa");
                }
                context.ExecuteQuery();
            }
        }

        private ViewCreationInformation GetViewCreationInfoFromConfig(ShView view)
        {
            return new ViewCreationInformation
            {
                Title = view.Title,
                Query = view.Query,
                ViewFields = view.ViewFields,
                RowLimit = view.RowLimit,
                SetAsDefaultView = view.DefaultView
            };
        }

        private ListCreationInformation GetListCreationInfoFromConfig(ShList listConfig)
        {
            return new ListCreationInformation
            {
                Description = listConfig.Description,
                Title = listConfig.Title,
                TemplateType = listConfig.TemplateType,
                Url = listConfig.Url
            };
        }
    }
}

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
            setupList.OnQuickLaunch = listConfig.OnQuickLaunch;
            setupList.EnableVersioning = listConfig.VersioningEnabled;
            setupList.Update();

            SetupContentTypesOfList(context, setupList, listConfig);

            SetupViewsOfList(context, setupList, listConfig);
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

                //Removing content types that are not in the configuration
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
                var serverRelativeUrl = UriUtilities.CombineServerRelativeUri(list.ParentWebUrl, view.Url);
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

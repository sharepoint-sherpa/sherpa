using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.SiteHierarchy
{
    public class ListManager
    {
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

            context.Load(setupList.Views);
            context.ExecuteQuery();

            foreach (ShView view in listConfig.Views)
            {
                SetupView(context, setupList, view);
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
                setupView.JSLink = view.JSLink;
                setupView.Update();
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

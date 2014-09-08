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

            var existingList = listCollection.FirstOrDefault(l => l.Title == listConfig.Title);
            if (existingList == null)
            {
                var listCreationInfo = GetListCreationInfoFromConfig(listConfig);
                existingList = listCollection.Add(listCreationInfo);
                context.ExecuteQuery();
            }
            existingList.OnQuickLaunch = listConfig.OnQuickLaunch;
            existingList.EnableVersioning = listConfig.VersioningEnabled;
            //TODO: Setup views
            existingList.Update();
            context.ExecuteQuery();
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

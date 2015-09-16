using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;

namespace Sherpa.Library.SiteHierarchy
{
    public class SearchNavigationManager
    {
        internal static void CreateSearchNavigationNodes(ClientContext clientContext, Web web, Dictionary<string, string> searchNavigationNodes)
        {
            if (searchNavigationNodes == null || searchNavigationNodes.Count == 0) return;

            var searchNavigation = web.Navigation.GetNodeById(1040);
            NavigationNodeCollection nodeCollection = searchNavigation.Children;

            clientContext.Load(searchNavigation);
            clientContext.Load(nodeCollection);
            clientContext.ExecuteQuery();

            for (int i = nodeCollection.Count - 1; i >= 0; i--)
            {
                nodeCollection[i].DeleteObject();
            }


            foreach (KeyValuePair<string, string> newNode in searchNavigationNodes)
            {
                nodeCollection.Add(new NavigationNodeCreationInformation
                {
                    Title = newNode.Key,
                    Url = newNode.Value
                });
            }

            clientContext.ExecuteQuery();           
        }
    }
}

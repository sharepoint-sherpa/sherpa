using System;
using System.Linq;
using Microsoft.SharePoint.Client;
using Sherpa.Library.API;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.ExampleCustomTask
{
    public class ExampleCustomTask : ITask 
    {
        public void ExecuteOn(ShWeb shweb, ClientContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            var web = context.Site.RootWeb;
            web.Title = "This site has been updated by a custom task";
            web.Update();
            context.ExecuteQuery();
        }
    }
}

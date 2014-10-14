using System.Xml;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Search.Administration;
using Microsoft.SharePoint.Client.Search.Portability;

namespace Sherpa.Library.Deploy
{
    public class SearchImportManager
    {
        public void ImportSearchConfiguration(ClientContext context, string pathToSearchXml)
        {
            var searchConfigurationPortability = new SearchConfigurationPortability(context);
            var owningScope = new SearchObjectOwner(context, SearchObjectLevel.SPSite);

            var configurationXml = new XmlDocument();
            configurationXml.Load(pathToSearchXml);

            searchConfigurationPortability.ImportSearchConfiguration(owningScope, configurationXml.OuterXml);
            context.ExecuteQuery();
        }
    }
}

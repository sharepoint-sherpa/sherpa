using System.IO;
using System.Reflection;
using Flurl;
using log4net;
using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.SiteHierarchy
{
    public class ComposedLookManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void SetComposedLook(ClientContext context, ShWeb configWeb, Web web, ShComposedLook composedLook)
        {
            if (composedLook != null)
            {
                Log.Debug("Setting Composed Look for web " + configWeb.Name);
                var themeUrl = string.Empty;
                var fontSchemeUrl = string.Empty;

                List themeList = web.GetCatalog(124);
                web.Context.Load(themeList);
                web.Context.ExecuteQuery();

                // We are assuming that the theme exists
                string camlString = @"
                <View>
                    <Query>                
                        <Where>
                            <Eq>
                                <FieldRef Name='Name' />
                                <Value Type='Text'>{0}</Value>
                            </Eq>
                        </Where>
                        </Query>
                </View>";
                camlString = string.Format(camlString, composedLook.Name);

                CamlQuery query = new CamlQuery();
                query.ViewXml = camlString;
                var themeItems = themeList.GetItems(query);
                web.Context.Load(themeItems);
                web.Context.ExecuteQuery();

                if (themeItems.Count == 0)
                {
                    if (!web.IsObjectPropertyInstantiated("ServerRelativeUrl"))
                    {
                        context.Load(web, w => w.ServerRelativeUrl);
                        context.ExecuteQuery();
                    }

                    var itemInfo = new ListItemCreationInformation();
                    ListItem item = themeList.AddItem(itemInfo);
                    item["Name"] = composedLook.Name;
                    item["Title"] = composedLook.Title;
                    if (!string.IsNullOrEmpty(composedLook.ThemeUrl))
                    {
                        themeUrl = Url.Combine(web.ServerRelativeUrl, string.Format("/_catalogs/theme/15/{0}", Path.GetFileName(composedLook.ThemeUrl)));
                        item["ThemeUrl"] = themeUrl;
                    }
                    if (!string.IsNullOrEmpty(composedLook.FontSchemeUrl))
                    {
                        fontSchemeUrl = Url.Combine(web.ServerRelativeUrl, string.Format("/_catalogs/theme/15/{0}", Path.GetFileName(composedLook.FontSchemeUrl)));
                        item["FontSchemeUrl"] = fontSchemeUrl;
                    }
                    if (string.IsNullOrEmpty(composedLook.MasterPageUrl))
                    {
                        item["MasterPageUrl"] = Url.Combine(web.ServerRelativeUrl, "/_catalogs/masterpage/seattle.master");
                    }
                    else
                    {
                        item["MasterPageUrl"] = Url.Combine(web.ServerRelativeUrl, string.Format("/_catalogs/masterpage/{0}", Path.GetFileName(composedLook.MasterPageUrl)));
                    }
                    item["DisplayOrder"] = 11;
                    item.Update();
                    context.ExecuteQuery();
                }
                else
                {
                    ListItem item = themeItems[0];
                    var themeUrlFieldValue = item["ThemeUrl"] as FieldUrlValue;
                    var fontSchemeUrlFieldValue = item["FontSchemeUrl"] as FieldUrlValue;
                    if (themeUrlFieldValue != null) themeUrl = UriUtilities.GetRelativeUrl(themeUrlFieldValue.Url);
                    if (fontSchemeUrlFieldValue != null) fontSchemeUrl = UriUtilities.GetRelativeUrl(fontSchemeUrlFieldValue.Url);
                }

                web.ApplyTheme(themeUrl, fontSchemeUrl, null, false);
                context.ExecuteQuery();
            }

        }
    }
}
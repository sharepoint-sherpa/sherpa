using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using Sherpa.Library.SiteHierarchy.Model;
using System.IO;

namespace Sherpa.Library.SiteHierarchy
{
    public class ExportTaskManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void ExportData(ClientContext context, Web parentWeb, ShWeb configWeb, string outputDirectory)
        {
            var webToConfigure = GetWeb(context, parentWeb, configWeb);

            ExportListData(context, webToConfigure, configWeb.Lists, outputDirectory);

            foreach (ShWeb subWeb in configWeb.Webs)
            {
                ExportData(context, webToConfigure, subWeb, outputDirectory);
            }
        }
        public void ExportListData(ClientContext context, Web web, List<ShList> listConfigs, string outputDirectory)
        {
            foreach (var listConfig in listConfigs)
            {
                if (listConfig.ExportData)
                {
                    var list = web.Lists.GetByTitle(listConfig.Title);
                    var items = list.GetItems(new CamlQuery());
                    context.Load(list, l => l.Title, l => l.BaseType);
                    if (listConfig.TemplateType == 171)
                    {
                        context.Load(items, includes => includes.Include(i => i["ID"], i => i["Title"], i => i["ParentID"], i => i["GtProjectPhase"], i => i["Order"]));
                    } else
                    {
                        context.Load(items, includes => includes.Include(i => i["ID"], i => i["Title"], i => i["GtProjectPhase"]));
                    }
                    
                    context.ExecuteQuery();

                    var listDataRows = new List<ShTaskListDataItem>();

                    foreach (var item in items)
                    {

                        if (listConfig.TemplateType == 171)
                        {
                            var phaseValue = item["GtProjectPhase"] as TaxonomyFieldValue;
                            var parentIdValue = item["ParentID"] as FieldLookupValue;
                            var parentId = parentIdValue != null ? parentIdValue.LookupId : 0;
                            var order = item["Order"];

                            var taskItem = new ShTaskListDataItem(int.Parse(item["ID"].ToString()), parentId);
                            taskItem.Order = double.Parse(order.ToString());
                            taskItem.Fields.Add(new ShFieldValue("Title", item["Title"].ToString()));
                            if (phaseValue != null) taskItem.Fields.Add(new ShFieldValue("GtProjectPhase", string.Format("{0}|{1}", phaseValue.Label, phaseValue.TermGuid)));

                            listDataRows.Add(taskItem);
                        }
                        else
                        {
                            var taskItem = new ShTaskListDataItem(int.Parse(item["ID"].ToString()));
                            taskItem.Fields.Add(new ShFieldValue("Title", item["Title"].ToString()));

                            var phaseValue = item["GtProjectPhase"] as TaxonomyFieldValue;
                            if (phaseValue != null) taskItem.Fields.Add(new ShFieldValue("GtProjectPhase", string.Format("{0}|{1}", phaseValue.Label, phaseValue.TermGuid)));

                            listDataRows.Add(taskItem);
                        }
                    
                    }
                    listDataRows.Sort((x, y) => x.Order.CompareTo(y.Order));
                    foreach (var item in listDataRows.Where(i => i.ParentID != 0))
                    {
                        listDataRows.Single(i => i.ID == item.ParentID).Rows.Add(item);
                    }
                    var itemsToPersist = new List<ShTaskListDataItem>();
                    foreach (var item in listDataRows)
                    {
                        if (item.ParentID == 0) itemsToPersist.Add(item);
                    }

                    var listData = new ShTaskListData();
                    listData.Data.Rows = itemsToPersist;
                    listData.Name = list.Title;
                    listData.Type = list.BaseType.ToString();
                    var taxPersistanceProvider = new FilePersistanceProvider<ShListData>(Path.Combine(outputDirectory, String.Format("{0}-export-{1}.json", list.Title.ToLower().Replace(" ", ""), System.DateTime.Now.ToFileTime())));
                    taxPersistanceProvider.Save(listData);
                }
            }
        }
        private Web GetWeb(ClientContext context, Web parentWeb, ShWeb configWeb)
        {
            Log.Debug("Getting web with url " + configWeb.Url);
            Web webToConfigure;
            if (parentWeb == null)
            {
                //We assume that the root web always exists
                webToConfigure = context.Site.RootWeb;
            }
            else
            {
                webToConfigure = GetSubWeb(context, parentWeb, configWeb.Url);

                if (webToConfigure == null)
                {
                    throw new Exception("Web does not exist");
                }
            }
            context.Load(webToConfigure, w => w.Url);
            context.ExecuteQuery();

            return webToConfigure;
        }

        private Web GetSubWeb(ClientContext context, Web parentWeb, string webUrl)
        {
            context.Load(parentWeb, w => w.Url, w => w.Webs);
            context.ExecuteQuery();

            var absoluteUrlToCheck = parentWeb.Url.TrimEnd('/') + '/' + webUrl;
            // use a simple linq query to get any sub webs with the URL we want to check
            return (from w in parentWeb.Webs where w.Url == absoluteUrlToCheck select w).SingleOrDefault();
        }
    }
}

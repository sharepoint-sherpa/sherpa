using System;
using Sherpa.Library.SiteHierarchy.Model;
using Microsoft.SharePoint.Client;
using log4net;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;

namespace Sherpa.Library.SiteHierarchy
{
    public class ImportDataManager
    {
        private static bool AvoidThrottling;
        private static int ThrottlingTreshold;
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private ClientContext Context { get; set; }
        public ImportDataManager(ClientContext context, int throttlingTreshhold = 0)
        {
            Context = context;

            if (throttlingTreshhold > 0)
            {
                AvoidThrottling = true;
                ThrottlingTreshold = throttlingTreshhold;
            }
        }
        public void ImportListData(ShListData listData)
        {
            Log.InfoFormat("Importing list data to list {0}", listData.Name);

            var web = Context.Web;
            var list = web.Lists.GetByTitle(listData.Name);
            Context.Load(list);
            Context.ExecuteQuery();

            if (list.ItemCount == 0)
            {
                ImportRow(list, listData.Data.Rows);
            }
        }

        public void ImportRow(List list, List<ShListDataItem> dataRows)
        {
            if (AvoidThrottling)
            {
                Thread.Sleep(ThrottlingTreshold);
            }

            foreach (var item in dataRows)
            {
                if (item.Fields.Count > 0)
                {
                    var newItemInfo = new ListItemCreationInformation();
                    var newItem = list.AddItem(newItemInfo);

                    foreach (var fieldValue in item.Fields)
                    {
                        newItem.ParseAndSetFieldValue(fieldValue.Name, fieldValue.Value);
                    }
                    newItem.Update();
                    Context.Load(newItem);
                    Context.ExecuteQuery();
                }
            }
        }
        public void ImportTaskListData(ShTaskListData listData)
        {
            Log.InfoFormat("Importing task list data to list {0}", listData.Name);

            var web = Context.Web;
            var list = web.Lists.GetByTitle(listData.Name);
            Context.Load(list);
            Context.ExecuteQuery();

            if (list.ItemCount == 0)
            {
                ImportTaskRow(list, listData.Data.Rows);
            }
        }
        public void ImportTaskRow(List list, List<ShTaskListDataItem> dataRows, ListItem parentItem = null)
        {
            if (AvoidThrottling)
            {
                Thread.Sleep(ThrottlingTreshold);
            }

            foreach (var item in dataRows)
            {
                if (item.Fields.Count > 0)
                {
                    var newItemInfo = new ListItemCreationInformation();
                    var newItem = list.AddItem(newItemInfo);

                    foreach (var fieldValue in item.Fields)
                    {
                        newItem.ParseAndSetFieldValue(fieldValue.Name, fieldValue.Value);
                    }
                    if (parentItem != null && parentItem.Id > 0)
                    {
                        newItem["ParentID"] = parentItem.Id;
                    }
                    newItem.Update();
                    Context.Load(newItem);
                    Context.ExecuteQuery();
                    
                    if (item.Rows.Count > 0)
                    {
                        ImportTaskRow(list, item.Rows, newItem);
                    }
                }
            }
        }
    }
}

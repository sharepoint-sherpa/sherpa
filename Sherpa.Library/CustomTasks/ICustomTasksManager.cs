using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using Sherpa.Library.API;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.CustomTasks
{
    interface ICustomTasksManager
    {
        void ExecuteTasks(ShWeb rootWeb, ClientContext context);
    }
}

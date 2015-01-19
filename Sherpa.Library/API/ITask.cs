using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;

namespace Sherpa.Library.API
{
    public interface ITask
    {
        void ExecuteOn(ShWeb web, ClientContext context);
    }
}

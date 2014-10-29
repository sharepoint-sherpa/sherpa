using Microsoft.SharePoint.Client;

namespace Sherpa.Library.Deploy
{
    interface IDeployManager
    {
        void UploadDesignPackageToSiteAssets(ClientContext context, string localFilePath);
        void ActivateDesignPackage(ClientContext context, string nameOfPackage, string siteRelativeUrlToLibrary);
    }
}

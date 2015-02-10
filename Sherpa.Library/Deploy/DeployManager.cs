using System;
using System.IO;
using System.Net;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Publishing;
using File = System.IO.File;

namespace Sherpa.Library.Deploy
{
    public class DeployManager : IDeployManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICredentials _credentials;
        private readonly Uri _urlToWeb;
        private readonly bool _isSharePointOnline;

        public DeployManager(Uri urlToWeb, ICredentials credentials, bool isSharePointOnline)
        {
            _urlToWeb = urlToWeb;
            _credentials = credentials;
            _isSharePointOnline = isSharePointOnline;
        }

        /// <summary>
        /// Uploads a design package to a library. Can be used for uploading sandboxed solutions to solution gallery.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="localFilePath">Path to package (wsp)</param>
        public void UploadDesignPackageToSiteAssets(ClientContext context, string localFilePath)
        {
                var fileName = Path.GetFileName(localFilePath);
                var fileExtension = Path.GetExtension(fileName);
                if (fileExtension != null && fileExtension.ToLower() != ".wsp")
                    throw new NotSupportedException("Only WSPs can be uploaded into the SharePoint solution store. " +
                                                    localFilePath + " is not a wsp");
                if (string.IsNullOrEmpty(fileName) || _urlToWeb == null)
                {
                    throw new Exception("Could not create path to solution package!");
                }

                var siteAssetsLibrary = context.Web.Lists.EnsureSiteAssetsLibrary();

                context.Load(siteAssetsLibrary);
                context.Load(siteAssetsLibrary.RootFolder);
                context.ExecuteQuery();

                if (_isSharePointOnline)
                {
                    var fileUrl = UriUtilities.CombineAbsoluteUri(_urlToWeb.GetLeftPart(UriPartial.Authority), siteAssetsLibrary.RootFolder.ServerRelativeUrl, fileName);
                    UploadFileToSharePointOnline(context, localFilePath, fileUrl);
                }
                else
                {
                    UploadFileToSharePointOnPrem(context, localFilePath, fileName);
                }
        }

        private void UploadFileToSharePointOnPrem(ClientContext context, string localFilePath, string fileName)
        {
            Log.InfoFormat("Uploading package {0} to library ", Path.GetFileName(localFilePath));
            try
            {
                var siteAssetsLibrary = context.Web.Lists.EnsureSiteAssetsLibrary();

                using (var fs = new FileStream(localFilePath, FileMode.Open))
                {
                    var fi = new FileInfo(fileName);
                    context.Load(siteAssetsLibrary.RootFolder);
                    context.ExecuteQuery();
                    var fileUrl = String.Format("{0}/{1}", siteAssetsLibrary.RootFolder.ServerRelativeUrl, fi.Name);

                    Microsoft.SharePoint.Client.File.SaveBinaryDirect(context, fileUrl, fs, true);
                }
            }
            catch (UnauthorizedAccessException)
            {
                Log.Error("Unauthorized to access " + localFilePath);
            }
        }

        private void UploadFileToSharePointOnline(ClientContext context, string localPath, string fileUrl)
        {
            Log.InfoFormat("Uploading package {0} to library ", Path.GetFileName(localPath));

            context.RequestTimeout = 1000000;
            context.Credentials = _credentials;

            var siteAssetsLibrary = context.Web.Lists.EnsureSiteAssetsLibrary();
            using (var fs = new FileStream(localPath, FileMode.Open))
            {
                var flciNewFile = new FileCreationInformation
                {
                    ContentStream = fs,
                    Url = Path.GetFileName(fileUrl),
                    Overwrite = true
                };
                Microsoft.SharePoint.Client.File uploadFile = siteAssetsLibrary.RootFolder.Files.Add(flciNewFile);
                context.Load(uploadFile);
                context.ExecuteQuery();
            }
        }



        /// <summary>
        /// Activates a design package based on package name
        /// Starting point: http://sharepoint.stackexchange.com/questions/90809/is-it-possible-to-activate-a-solution-using-client-code-in-sharepoint-online-201
        /// </summary>
        /// <param name="context"></param>
        /// <param name="filePathOrName">The filename of the package</param>
        /// <param name="siteRelativeUrlToLibrary">Site relative URL to the library of the package</param>
        public void ActivateDesignPackage(ClientContext context, string filePathOrName, string siteRelativeUrlToLibrary)
        {
            // if we pass in a full path, correct this
            var nameOfPackage = Path.GetFileNameWithoutExtension(filePathOrName);

            context.Load(context.Site);
            context.Load(context.Web);
            context.ExecuteQuery();

            var stagedFileUrl = UriUtilities.CombineServerRelativeUri(context.Site.ServerRelativeUrl, siteRelativeUrlToLibrary, nameOfPackage + ".wsp");
            var packageInfo = GetPackageInfoWithLatestVersion(context, nameOfPackage, stagedFileUrl);

            Log.Info("Installing solution package " + GetFileNameFromPackageInfo(packageInfo));
            DesignPackage.Install(context, context.Site, packageInfo, stagedFileUrl);
            context.ExecuteQuery();

            DeleteFile(context, stagedFileUrl);
        }

        private DesignPackageInfo GetPackageInfoWithLatestVersion(ClientContext context, string nameOfPackage, string fileUrl)
        {
            var web = context.Web;
            var stagedFile = web.GetFileByServerRelativeUrl(fileUrl);
            context.Load(stagedFile, f => f.Exists, f => f.Name);
            context.ExecuteQuery();
            if (stagedFile.Exists)
            {
                return GetPackageInfoWithFirstAvailableMinorVersion(context, nameOfPackage, 1, 0);
            }
            return null;
        }

        private DesignPackageInfo GetPackageInfoWithFirstAvailableMinorVersion(ClientContext context, string nameOfPackage, int majorVersion, int minorVersion)
        {
            var newVersionPackageInfo = GetPackageInfo(nameOfPackage, majorVersion, minorVersion);

            var nameInSolutionGallery = GetFileNameFromPackageInfo(newVersionPackageInfo);
            var serverRelativeUri = UriUtilities.CombineServerRelativeUri(context.Site.ServerRelativeUrl, "/_catalogs/solutions/", nameInSolutionGallery);
            var fileInSolutionGallery = context.Web.GetFileByServerRelativeUrl(serverRelativeUri);
            context.Load(fileInSolutionGallery, f => f.Exists);
            context.ExecuteQuery();

            return !fileInSolutionGallery.Exists ? newVersionPackageInfo : GetPackageInfoWithFirstAvailableMinorVersion(context, nameOfPackage, majorVersion, minorVersion+1);
        }

        private DesignPackageInfo GetPackageInfo(string nameOfPackage, int majorVersion, int minorVersion)
        {
            return new DesignPackageInfo
            {
                PackageName = nameOfPackage,
                MajorVersion = majorVersion,
                MinorVersion = minorVersion
            };
        }

        public bool IsCurrentUserSiteCollectionAdmin(ClientContext context)
        {
            var currentUser = context.Web.CurrentUser;
            context.Load(currentUser, u => u.IsSiteAdmin);
            context.ExecuteQuery();

            return currentUser.IsSiteAdmin;
        }

        /// <summary>
        /// This is how SharePoint creates the name of the package that is installed
        /// </summary>
        /// <param name="packageInfo"></param>
        /// <returns></returns>
        private static string GetFileNameFromPackageInfo(DesignPackageInfo packageInfo)
        {
            return string.Format("{0}-v{1}.{2}.wsp", packageInfo.PackageName, packageInfo.MajorVersion, packageInfo.MinorVersion);
        }

        private static void DeleteFile(ClientContext context, string fileUrl)
        {
            var web = context.Web;
            var file = web.GetFileByServerRelativeUrl(fileUrl);
            context.Load(file);
            file.DeleteObject();
            context.ExecuteQuery();
        }

        public void ForceRecrawl()
        {
            using (var context = new ClientContext(_urlToWeb))
            {
                context.Credentials = _credentials;
                context.Load(context.Web);
                context.ExecuteQuery();
                ForceRecrawlOf(context.Web, context);

            }
        }

        private void ForceRecrawlOf(Web web, ClientContext context)
        {
            Log.Info("Scheduling full recrawl of: " + web.Url);
            context.Credentials = _credentials;

            context.Load(web, x => x.AllProperties, x => x.Webs);
            context.ExecuteQuery();
            var version = 0;
            var subWebs = web.Webs;

            var allProperties = web.AllProperties;
            if (allProperties.FieldValues.ContainsKey("vti_searchversion"))
            {
                version = (int)allProperties["vti_searchversion"];
            }
            version++;
            allProperties["vti_searchversion"] = version;
            web.Update();
            context.ExecuteQuery();
            foreach (var subWeb in subWebs)
            {
                ForceRecrawlOf(subWeb, context);
            }
        }
    }
}

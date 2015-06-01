using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.WebParts;
using Sherpa.Library.SiteHierarchy.Model;
using Flurl;
using File = Microsoft.SharePoint.Client.File;

namespace Sherpa.Library.SiteHierarchy
{
    public class ContentUploadManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly string _contentDirectoryPath;
        public ContentUploadManager(string rootConfigurationPath)
        {
            _contentDirectoryPath = rootConfigurationPath;
        }

        public void UploadFilesInFolder(ClientContext context, Web web, List<ShContentFolder> contentFolders)
        {
            foreach (ShContentFolder folder in contentFolders)
            {
                UploadFilesInFolder(context, web, folder);
            }
        }

        public void UploadFilesInFolder(ClientContext context, Web web, ShContentFolder contentFolder)
        {
            Log.Info("Uploading files from contentfolder " + contentFolder.FolderName);
            
            var assetLibrary = web.Lists.GetByTitle(contentFolder.ListName);
            context.Load(assetLibrary, l => l.RootFolder);
            context.ExecuteQuery();

            var uploadTargetFolder = Url.Combine(assetLibrary.RootFolder.ServerRelativeUrl, contentFolder.FolderUrl);
            var configRootFolder = Path.Combine(_contentDirectoryPath, contentFolder.FolderName);

            if (!web.DoesFolderExists(uploadTargetFolder))
            {
                web.Folders.Add(uploadTargetFolder);
            }
            context.ExecuteQuery();

            foreach (string folder in Directory.GetDirectories(configRootFolder, "*", SearchOption.AllDirectories))
            {
                var folderName = Url.Combine(uploadTargetFolder, folder.Replace(configRootFolder, "").Replace("\\", "/"));
                if (!web.DoesFolderExists(folderName))
                {
                    web.Folders.Add(folderName);
                }
            }
            context.ExecuteQuery();

            List<ShFileProperties> filePropertiesCollection = null;
            if (!string.IsNullOrEmpty(contentFolder.PropertiesFile))
            {
                var propertiesFilePath = Path.Combine(configRootFolder, contentFolder.PropertiesFile);
                var filePersistanceProvider = new FilePersistanceProvider<List<ShFileProperties>>(propertiesFilePath);
                filePropertiesCollection = filePersistanceProvider.Load();
            }

            context.Load(context.Site, site => site.ServerRelativeUrl);
            context.Load(context.Web, w => w.ServerRelativeUrl, w => w.Language);
            context.ExecuteQuery();

            foreach (string filePath in Directory.GetFiles(configRootFolder, "*", SearchOption.AllDirectories))
            {
                var pathToFileFromRootFolder = filePath.Replace(configRootFolder.TrimEnd(new []{'\\'}) + "\\", "");
                var fileName = Path.GetFileName(pathToFileFromRootFolder);

                if (!string.IsNullOrEmpty(contentFolder.PropertiesFile) && contentFolder.PropertiesFile == fileName)
                {
                    Log.DebugFormat("Skipping file upload of {0} since it's used as a configuration file", fileName);
                    continue;
                }
                
                var fileUrl = GetFileUrl(uploadTargetFolder, pathToFileFromRootFolder, filePropertiesCollection);
                
                var newFile = new FileCreationInformation
                {
                    Content = System.IO.File.ReadAllBytes(filePath),
                    Url = fileUrl,
                    Overwrite = true
                };
                
                File uploadFile = assetLibrary.RootFolder.Files.Add(newFile);
                
                context.Load(uploadFile);
                context.Load(uploadFile.ListItemAllFields.ParentList, l => l.ForceCheckout, l => l.EnableMinorVersions, l => l.EnableModeration);
                context.ExecuteQuery();

                ApplyFileProperties(context, filePropertiesCollection, uploadFile);
            }
        }

        private string GetFileUrl(string uploadTargetFolder, string pathToFileFromRootFolder,
            IEnumerable<ShFileProperties> filePropertiesCollection)
        {
            pathToFileFromRootFolder = pathToFileFromRootFolder.Replace("\\", "/");
            var fileUrl = Url.Combine(uploadTargetFolder, pathToFileFromRootFolder);

            if (filePropertiesCollection != null)
            {
                var fileProperties = filePropertiesCollection.SingleOrDefault(f => f.Path == pathToFileFromRootFolder);
                if (fileProperties != null)
                {
                    fileUrl = Url.Combine(uploadTargetFolder, fileProperties.Url);
                }
            }
            return fileUrl;
        }

        private void ApplyFileProperties(ClientContext context, IEnumerable<ShFileProperties> filePropertiesCollection, File uploadFile)
        {
            if (filePropertiesCollection != null)
            {
                var fileProperties = filePropertiesCollection.SingleOrDefault(f => f.Path == uploadFile.Name);
                if (fileProperties != null)
                {
                    var filePropertiesWithTokensReplaced = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, string> keyValuePair in fileProperties.Properties)
                    {
                        filePropertiesWithTokensReplaced.Add(keyValuePair.Key, GetPropertyValueWithTokensReplaced(keyValuePair.Value, context));
                    }
                    uploadFile.SetFileProperties(filePropertiesWithTokensReplaced);

                    if (uploadFile.Name.ToLower().EndsWith(".aspx")) 
                        AddWebParts(context, uploadFile, fileProperties.WebParts, fileProperties.ReplaceWebParts);
                    uploadFile.PublishFileToLevel(fileProperties.Level);
                    context.ExecuteQuery();
                }
            }
        }

        public string GetPropertyValueWithTokensReplaced(string valueWithTokens, ClientContext context)
        {
            return valueWithTokens
                .Replace("~SiteCollection", context.Site.ServerRelativeUrl)
                .Replace("~Site", context.Web.ServerRelativeUrl)
                .Replace("$Resources:core,Culture;", new CultureInfo((int)context.Web.Language).Name);
        }

        public void AddWebParts(ClientContext context, File uploadFile, List<ShWebPartReference> webPartReferences, bool replaceWebParts)
        {
            // we should be allowed to delete webparts (by using replaceWebparts and define no new ones
            if (webPartReferences.Count <= 0 && !replaceWebParts) return;

            var limitedWebPartManager = uploadFile.GetLimitedWebPartManager(PersonalizationScope.Shared);

            context.Load(limitedWebPartManager, manager => manager.WebParts);
            context.ExecuteQuery();

            if (limitedWebPartManager.WebParts.Count == 0 || replaceWebParts)
            {
                for (int i = limitedWebPartManager.WebParts.Count - 1; i >= 0; i--)
                {
                    limitedWebPartManager.WebParts[i].DeleteWebPart();
                }
                context.ExecuteQuery();

                foreach (ShWebPartReference webPart in webPartReferences)
                {
                    //Convention: All webparts are located in the content/webparts folder
                    var webPartPath = Path.Combine(_contentDirectoryPath, "webparts", webPart.FileName);
                    var webPartFileContent = System.IO.File.ReadAllText(webPartPath);
                    if (!System.IO.File.Exists(webPartPath))
                    {
                        Log.ErrorFormat("Webpart at path {0} not found", webPartPath);
                        continue;
                    }
                    
                    var webPartDefinition = limitedWebPartManager.ImportWebPart(webPartFileContent);
                    if (webPart.PropertiesOverrides.Count > 0)
                    {
                        foreach (KeyValuePair<string, string> propertyOverride in webPart.PropertiesOverrides)
                        {
                            webPartDefinition.WebPart.Properties[propertyOverride.Key] = propertyOverride.Value;
                        }
                    }
                    limitedWebPartManager.AddWebPart(webPartDefinition.WebPart, webPart.ZoneID, webPart.Order);
                }

                context.Load(limitedWebPartManager);
                context.ExecuteQuery();
            }
        }
    }
}

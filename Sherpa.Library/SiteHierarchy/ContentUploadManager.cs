using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Web.Management;
using log4net;
using Microsoft.SharePoint.Client;
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

        public void UploadFilesInFolder(ClientContext context, Web web, List<ShContentFolder> configFolders)
        {
            foreach (ShContentFolder folder in configFolders)
            {
                UploadFilesInFolder(context, web, folder);
            }
        }

        public void UploadFilesInFolder(ClientContext context, Web web, ShContentFolder configFolder)
        {
            Log.Info("Uploading files from contentfolder " + configFolder.FolderName);
            
            var assetLibrary = web.Lists.GetByTitle(configFolder.ListName);
            context.Load(assetLibrary, l => l.RootFolder);
            context.ExecuteQuery();

            var uploadTargetFolder = Url.Combine(assetLibrary.RootFolder.ServerRelativeUrl, configFolder.FolderUrl);
            var configRootFolder = Path.Combine(_contentDirectoryPath, configFolder.FolderName);

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
            if (!string.IsNullOrEmpty(configFolder.PropertiesFile))
            {
                var propertiesFilePath = Path.Combine(configRootFolder, configFolder.PropertiesFile);
                var filePersistanceProvider = new FilePersistanceProvider<List<ShFileProperties>>(propertiesFilePath);
                filePropertiesCollection = filePersistanceProvider.Load();
            }

            context.Load(context.Site, site => site.ServerRelativeUrl);
            context.Load(context.Web, w => w.ServerRelativeUrl, w => w.Language);
            context.ExecuteQuery();

            foreach (string filePath in Directory.GetFiles(configRootFolder, "*", SearchOption.AllDirectories))
            {
                var pathToFileFromRootFolder = filePath.Replace(configRootFolder, "");
                var fileName = Path.GetFileName(pathToFileFromRootFolder);

                if (!string.IsNullOrEmpty(configFolder.PropertiesFile) && configFolder.PropertiesFile == fileName)
                {
                    Log.DebugFormat("Skipping file upload of {0} since it's used as a configuration file", fileName);
                    continue;
                }
                

                var fileUrl = GetFileUrl(uploadTargetFolder, pathToFileFromRootFolder, filePropertiesCollection, fileName);
                
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
            IEnumerable<ShFileProperties> filePropertiesCollection, string fileName)
        {
            var fileUrl = Url.Combine(uploadTargetFolder, pathToFileFromRootFolder.Replace("\\", "/"));

            if (filePropertiesCollection != null)
            {
                var fileProperties = filePropertiesCollection.SingleOrDefault(f => f.Path == fileName);
                if (fileProperties != null)
                {
                    fileUrl = Url.Combine(uploadTargetFolder, fileProperties.Url);
                }
            }
            return fileUrl;
        }

        private void ApplyFileProperties(ClientContext context, IEnumerable<ShFileProperties> filePropertiesCollection, File uploadFile)
        {
            var fileLevel = FileLevel.Published;
            if (filePropertiesCollection != null)
            {
                var fileProperties = filePropertiesCollection.SingleOrDefault(f => f.Path == uploadFile.Name);
                if (fileProperties != null)
                {
                    fileLevel = fileProperties.Level;
                    var item = uploadFile.ListItemAllFields;
                    context.Load(item);
                    foreach (KeyValuePair<string, string> property in fileProperties.Properties)
                    {
                        item[property.Key] = GetPropertyValueWithTokensReplaced(property.Value, context);
                    }
                    item.Update();
                }
            }
            uploadFile.PublishFileToLevel(fileLevel);
            context.ExecuteQuery();
        }

        public string GetPropertyValueWithTokensReplaced(string valueWithTokens, ClientContext context)
        {
            return valueWithTokens
                .Replace("~SiteCollection", context.Site.ServerRelativeUrl)
                .Replace("~Site", context.Web.ServerRelativeUrl)
                .Replace("$Resources:core,Culture;", new CultureInfo((int)context.Web.Language).Name);
        }
    }
}

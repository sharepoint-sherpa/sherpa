using System.Collections.Generic;
using System.IO;
using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;
using Flurl;

namespace Sherpa.Library.SiteHierarchy
{
    public class ContentUploadManager
    {
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
            if (configFolder == null) return;

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

            foreach (string filePath in Directory.GetFiles(configRootFolder, "*", SearchOption.AllDirectories))
            {
                var fileUrl = Url.Combine(uploadTargetFolder, filePath.Replace(configRootFolder, "").Replace("\\", "/"));
                var newFile = new FileCreationInformation
                {
                    Content = System.IO.File.ReadAllBytes(filePath),
                    Url = fileUrl,
                    Overwrite = true
                };
                Microsoft.SharePoint.Client.File uploadFile = assetLibrary.RootFolder.Files.Add(newFile);
                context.Load(uploadFile);
                context.ExecuteQuery();
            }
            
        }
    }
}

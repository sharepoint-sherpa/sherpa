using System.Collections.Generic;
using System.IO;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;
using Flurl;
using System.Xml.Serialization;
using System;

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

            ShWebPartCollection webParts = null;
            string path = Url.Combine(_contentDirectoryPath.Replace("\\", "/"), "manifest.xml");

            XmlSerializer serializer = new XmlSerializer(typeof(ShWebPartCollection));

            StreamReader reader = new StreamReader(path);
            webParts = (ShWebPartCollection) serializer.Deserialize(reader);
            reader.Close();

            Console.WriteLine(webParts.WebParts.Length);

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

                Microsoft.SharePoint.Client.WebParts.LimitedWebPartManager limitedWebPartManager = uploadFile.GetLimitedWebPartManager(Microsoft.SharePoint.Client.WebParts.PersonalizationScope.Shared);

                context.Load(limitedWebPartManager);
                context.ExecuteQuery();

                for (var i = 0; i < webParts.WebParts.Length; i++)
                {
                    var webPartXml = webParts.WebParts[i].Definition;
                    var webPartDef = limitedWebPartManager.ImportWebPart(webPartXml);
                    limitedWebPartManager.AddWebPart(webPartDef.WebPart, webParts.WebParts[i].WebPartZoneID, Int32.Parse(webParts.WebParts[i].WebPartOrder));
                }
                context.Load(uploadFile);
                context.Load(limitedWebPartManager);
                context.ExecuteQuery();
            }
            
        }
    }
}

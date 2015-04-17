﻿using System.Collections.Generic;
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

        private ShFileCollection GetManifestConfiguration(string folder)
        {
            ShFileCollection files = null;
            string manifestPath = Url.Combine(folder.Replace("\\", "/"), "manifest.xml");

            if (System.IO.File.Exists(manifestPath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ShFileCollection));

                StreamReader reader = new StreamReader(manifestPath);
                files = (ShFileCollection)serializer.Deserialize(reader);
                reader.Close();
            }

            return files;
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

            ShFileCollection files = GetManifestConfiguration(configRootFolder);

            foreach (string filePath in Directory.GetFiles(configRootFolder, "*", SearchOption.AllDirectories))
            {
                if (filePath.Contains("manifest.xml")) { return; }

                var fileName = filePath.Split('\\')[filePath.Split('\\').Length - 1];
                var fileConfig = files.GetFileByName(fileName);
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

                for (var i = 0; i < fileConfig.WebParts.Length; i++)
                {
                    var wp = fileConfig.WebParts[i];
                    var webPartDef = limitedWebPartManager.ImportWebPart(wp.Definition);
                    limitedWebPartManager.AddWebPart(webPartDef.WebPart, wp.WebPartZoneID, Int32.Parse(wp.WebPartOrder));
                }
                context.Load(limitedWebPartManager);
                context.ExecuteQuery();
            }         
        }
    }
}

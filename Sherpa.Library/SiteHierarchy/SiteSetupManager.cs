using System;
using System.IO;
using System.Linq;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Sherpa.Library.SiteHierarchy.Model;
using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy
{
    public class SiteSetupManager : ISiteSetupManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public readonly ShSiteCollection ConfigurationSiteCollection;
        private ClientContext ClientContext { get; set; }
        private FeatureManager FeatureManager { get; set; }
        private QuicklaunchManager QuicklaunchManager { get; set; }
        private PropertyManager PropertyManager { get; set; }
        private ListManager ListManager { get; set; }
        private ContentUploadManager ContentUploadManager { get; set; }

        public SiteSetupManager(ClientContext clientContext, ShSiteCollection configurationSiteCollection, string rootConfigurationPath)
        {
            ConfigurationSiteCollection = configurationSiteCollection;
            ClientContext = clientContext;

            FeatureManager = new FeatureManager();
            QuicklaunchManager = new QuicklaunchManager();
            PropertyManager = new PropertyManager();
            ListManager = new ListManager();

            var contentConfigurationPath = Path.Combine(rootConfigurationPath, "content");
            ContentUploadManager = new ContentUploadManager(contentConfigurationPath);
        }
        public void SetupSites()
        {
            Log.Debug("Starting SetupSites - setting up site collection");
            SetUpCustomActions(ClientContext, ConfigurationSiteCollection.CustomActions);
            SetUpCustomPermissionLevels(ClientContext, ConfigurationSiteCollection.PermissionLevels);
            FeatureManager.ActivateSiteCollectionFeatures(ClientContext, ConfigurationSiteCollection.SiteFeatures);
            EnsureAndConfigureWebAndActivateFeatures(ClientContext, null, ConfigurationSiteCollection.RootWeb);
        }
        public void SetUpCustomActions(ClientContext context, List<ShCustomAction> customActions)
        {
            Site site = context.Site;
            context.Load(site);
            context.Load(site.UserCustomActions);
            context.ExecuteQuery();

            foreach (var customAction in site.UserCustomActions)
            {
                customAction.DeleteObject();
                context.ExecuteQuery();
            }
            foreach (var customAction in customActions)
            {
                if (customAction.Location == null)
                {
                    Log.Info("You need to specify Location for your Custom Action.");
                    continue;
                }

                Log.InfoFormat("Adding custom action at Location '{0}'", customAction.Location);

                UserCustomAction userCustomAction = site.UserCustomActions.Add();
                userCustomAction.Location = customAction.Location;
                userCustomAction.ScriptSrc = customAction.ScriptSrc;
                userCustomAction.Sequence = customAction.Sequence;
                userCustomAction.Description = customAction.Description;
                userCustomAction.RegistrationId = customAction.RegistrationId;
                userCustomAction.RegistrationType = customAction.RegistrationType;
                userCustomAction.ScriptBlock = customAction.ScriptBlock;
                userCustomAction.Title = customAction.Title;
                userCustomAction.Name = customAction.Name;
                userCustomAction.ImageUrl = customAction.ImageUrl;
                userCustomAction.Group = customAction.Group;

                try {
                    userCustomAction.Update();
                    context.ExecuteQuery();
                    Log.Info("Custom action successfully added.");
                } catch(Exception e) {
                    Log.Error(e.Message);
                }
             }
        }


        public void SetUpCustomPermissionLevels(ClientContext context, List<ShPermissionLevel> permissionLevels)
        {
            foreach (var permissionLevel in permissionLevels)
            {
                context.Load(context.Site.RootWeb.RoleDefinitions);
                context.ExecuteQuery();

                var existingPermissionLevel = context.Site.RootWeb.RoleDefinitions.FirstOrDefault(x => x.Name.Equals(permissionLevel.Name));
                if (existingPermissionLevel == null)
                {
                    Log.Info("Creating permission level " + permissionLevel.Name);
                    BasePermissions permissions = new BasePermissions();
                    foreach (var basePermission in permissionLevel.BasePermissions)
                    {
                        permissions.Set(basePermission);
                    }
                    RoleDefinitionCreationInformation roleDefinitionCreationInfo = new RoleDefinitionCreationInformation();
                    roleDefinitionCreationInfo.BasePermissions = permissions;
                    roleDefinitionCreationInfo.Name = permissionLevel.Name;
                    roleDefinitionCreationInfo.Description = permissionLevel.Description;
                    RoleDefinition roleDefinition = context.Site.RootWeb.RoleDefinitions.Add(roleDefinitionCreationInfo);
                    context.ExecuteQuery();
                }
            }
        }

        /// <summary>
        /// Assumptions:
        /// 1. The order of webs and subwebs in the config file follows the structure of SharePoint sites
        /// 2. No config element is present without their parent web already being defined in the config file, except the root web
        /// </summary>
        private void EnsureAndConfigureWebAndActivateFeatures(ClientContext context, Web parentWeb, ShWeb configWeb)
        {
            var webToConfigure = EnsureWeb(context, parentWeb, configWeb);

            FeatureManager.ActivateWebFeatures(context, webToConfigure, configWeb.WebFeatures);
            ListManager.CreateLists(context, webToConfigure, configWeb.Lists);
            QuicklaunchManager.CreateQuicklaunchNodes(context, webToConfigure, configWeb.Quicklaunch);
            PropertyManager.SetProperties(context, webToConfigure, configWeb.Properties);
            ContentUploadManager.UploadFilesInFolder(context, webToConfigure, configWeb.ContentFolders);
            SetWelcomePageUrlIfConfigured(context, webToConfigure, configWeb);

            foreach (ShWeb subWeb in configWeb.Webs)
            {
                EnsureAndConfigureWebAndActivateFeatures(context, webToConfigure, subWeb);
            }
        }

        private void SetWelcomePageUrlIfConfigured(ClientContext context, Web webToConfigure, ShWeb configWeb)
        {
            if (!string.IsNullOrEmpty(configWeb.WelcomePageUrl))
            {
                var rootFolder = webToConfigure.RootFolder;
                rootFolder.WelcomePage = configWeb.WelcomePageUrl;
                rootFolder.Update();
                context.Load(webToConfigure.RootFolder);
                context.ExecuteQuery();
            }
        }

        private Web EnsureWeb(ClientContext context, Web parentWeb, ShWeb configWeb)
        {
            Log.Debug("Ensuring web with url " + configWeb.Url);
            Web webToConfigure;
            if (parentWeb == null)
            {
                //We assume that the root web always exists
                webToConfigure = context.Site.RootWeb;
            }
            else
            {
                webToConfigure = GetSubWeb(context, parentWeb, configWeb.Url);

                if (webToConfigure == null)
                {
                    Console.WriteLine("Creating web " + configWeb.Url);
                    webToConfigure = parentWeb.Webs.Add(GetWebCreationInformationFromConfig(configWeb));
                }
            }
            context.Load(webToConfigure, w => w.Url);
            context.ExecuteQuery();

            return webToConfigure;
        }

        private Web GetSubWeb(ClientContext context, Web parentWeb, string webUrl)
        {
            context.Load(parentWeb, w => w.Url, w => w.Webs);
            context.ExecuteQuery();

            var absoluteUrlToCheck = parentWeb.Url.TrimEnd('/') + '/' + webUrl;
            // use a simple linq query to get any sub webs with the URL we want to check
            return (from w in parentWeb.Webs where w.Url == absoluteUrlToCheck select w).SingleOrDefault();
        }

        /// <summary>
        /// Will only activate site collection features or rootweb's web features
        /// </summary>
        public void ActivateContentTypeDependencyFeatures()
        {
            FeatureManager.ActivateSiteCollectionFeatures(ClientContext, ConfigurationSiteCollection.SiteFeatures, true);
            FeatureManager.ActivateWebFeatures(ClientContext, ClientContext.Web, ConfigurationSiteCollection.RootWeb.WebFeatures, true);
        }

        private WebCreationInformation GetWebCreationInformationFromConfig(ShWeb configWeb)
        {
            return new WebCreationInformation
                {
                    Title = configWeb.Name,
                    Description = configWeb.Description,
                    Language = configWeb.Language,
                    Url = configWeb.Url,
                    UseSamePermissionsAsParentSite = true,
                    WebTemplate = configWeb.Template
                };
        }

        public static void DeleteSites(ClientContext clientContext)
        {
            clientContext.Load(clientContext.Site.RootWeb.Webs);
            clientContext.ExecuteQuery();

            var webs = clientContext.Site.RootWeb.Webs.ToList();

            foreach (var web in webs)
            {
                DeleteWeb(clientContext, web);
            }
        }

        private static void DeleteWeb(ClientContext clientContext, Web web)
        {
            clientContext.Load(web.Webs);
            clientContext.ExecuteQuery();

            foreach (Web subWeb in web.Webs)
            {
                DeleteWeb(clientContext, subWeb);
            }
            web.DeleteObject();
            clientContext.ExecuteQuery();
        }
    }
}

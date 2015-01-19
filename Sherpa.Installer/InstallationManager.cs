using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using log4net;
using Microsoft.SharePoint.Client;
using Sherpa.Library;
using Sherpa.Library.ContentTypes;
using Sherpa.Library.ContentTypes.Model;
using Sherpa.Library.CustomTasks;
using Sherpa.Library.Deploy;
using Sherpa.Library.SiteHierarchy;
using Sherpa.Library.SiteHierarchy.Model;
using Sherpa.Library.Taxonomy;
using Sherpa.Library.Taxonomy.Model;
using File = System.IO.File;

namespace Sherpa.Installer
{
    public class InstallationManager
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly ICredentials _credentials;
        private readonly Uri _urlToSite;
        private readonly bool _isSharePointOnline;
        private readonly string _rootPath;

        private string ConfigurationDirectoryPath
        {
            get { return Path.Combine(_rootPath, "config"); }
        }
        private string SolutionsDirectoryPath
        {
            get { return Path.Combine(_rootPath, "solutions"); }
        }
        private string SearchDirectoryPath
        {
            get { return Path.Combine(_rootPath, "search"); }
        }

        public InstallationManager(Uri urlToSite, ICredentials credentials, bool isSharePointOnline, string rootPath)
        {
            _urlToSite = urlToSite;
            _credentials = credentials;
            _isSharePointOnline = isSharePointOnline;
            _rootPath = rootPath ?? Environment.CurrentDirectory;

            Log.Debug("Installation manager created");
            Log.DebugFormat("Site Url: {0}, Configpath: {1}, SPO: {2}", _urlToSite.AbsoluteUri, _rootPath, _isSharePointOnline);
        }

        public void InstallOperation(InstallationOperation installationOperation)
        {
            InstallOperation(installationOperation, string.Empty);
        }

        public void InstallOperation(InstallationOperation installationOperation, string siteHierarchyFileName)
        {
            Log.Info("Executing operation " + installationOperation);

            if (installationOperation == InstallationOperation.Invalid || installationOperation == InstallationOperation.ExitApplication)
            {
                Log.Warn("Installation aborted based on user input");
                Environment.Exit(1);
                return;
            }
            var useConfigurationForInstall = false;
            var configurationFile = string.Empty;
            if (string.IsNullOrEmpty(siteHierarchyFileName))
            {
                Log.Info("No configuration file - convention mode enabled");
            }
            else
            {
                Log.Debug("Site configuration file: " + siteHierarchyFileName);
                configurationFile = Path.Combine(ConfigurationDirectoryPath, siteHierarchyFileName);
                useConfigurationForInstall = true;
                if (!File.Exists(configurationFile))
                {
                    Log.Fatal("Couldn't find the configuration file " + configurationFile);
                    throw new ArgumentException("Couldn't find the configuration file " + configurationFile);
                }
            }

            using (var context = new ClientContext(_urlToSite) {Credentials = _credentials})
            {
                var siteSetupManagerFromConfig = new SiteSetupManager(context, new ShSiteCollection());
                if (useConfigurationForInstall)
                {
                    var filePersistanceProvider = new FilePersistanceProvider<ShSiteCollection>(configurationFile);
                    siteSetupManagerFromConfig = new SiteSetupManager(context, filePersistanceProvider.Load());
                }
                switch (installationOperation)
                {
                    case InstallationOperation.ExecuteCustomTasks:
                    {
                        var customTasksManager = new CustomTasksManager(_rootPath);
                        customTasksManager.ExecuteTasks(siteSetupManagerFromConfig.ConfigurationSiteCollection.RootWeb, context);
                        break;
                    }
                    case InstallationOperation.InstallTaxonomy:
                    {
                        if (useConfigurationForInstall)
                        {
                            foreach (var filename in siteSetupManagerFromConfig.ConfigurationSiteCollection.TaxonomyConfigurations)
                            {
                                InstallTaxonomyFromSingleFile(context,
                                    Path.Combine(ConfigurationDirectoryPath, filename));
                            }
                        }
                        else
                        {
                            InstallAllTaxonomy(context);
                        }
                        break;
                    }
                    case InstallationOperation.UploadAndActivateSolution:
                    {
                        if (useConfigurationForInstall)
                        {
                            var deployManager = new DeployManager(_urlToSite, _credentials, _isSharePointOnline);
                            foreach (var filename in siteSetupManagerFromConfig.ConfigurationSiteCollection.SandboxedSolutions)
                            {
                                UploadAndActivatePackage(context, deployManager,
                                    Path.Combine(SolutionsDirectoryPath, filename));
                            }
                        }
                        else
                        {
                            UploadAndActivateAllSandboxSolutions(context);
                        }
                        break;
                    }
                    case InstallationOperation.InstallFieldsAndContentTypes:
                    {
                        if (useConfigurationForInstall)
                        {
                            siteSetupManagerFromConfig.ActivateContentTypeDependencyFeatures();
                            foreach (var fileName in siteSetupManagerFromConfig.ConfigurationSiteCollection.FieldConfigurations)
                            {
                                var filePath = Path.Combine(ConfigurationDirectoryPath, fileName);
                                CreateFieldsFromFile(context, filePath);
                            }
                            foreach (var fileName in siteSetupManagerFromConfig.ConfigurationSiteCollection.ContentTypeConfigurations)
                            {
                                var filePath = Path.Combine(ConfigurationDirectoryPath, fileName);
                                CreateContentTypesFromFile(context, filePath);
                            }
                        }
                        else
                        {
                            CreateAllSiteColumnsAndContentTypes(context);
                        }
                        break;
                    }
                    case InstallationOperation.ConfigureSites:
                    {
                        if (useConfigurationForInstall)
                        {
                            siteSetupManagerFromConfig.SetupSites();
                        }
                        else
                        {
                            ConfigureSitesFromAllSiteHierarchyFiles(context);
                        }
                        break;
                    }
                    case InstallationOperation.ImportSearch:
                    {
                        if (useConfigurationForInstall)
                        {
                            var searchMan = new SearchImportManager();
                            foreach (
                                var fileName in
                                    siteSetupManagerFromConfig.ConfigurationSiteCollection.SearchConfigurations)
                            {
                                var pathToSearchSettingsFile = Path.Combine(SearchDirectoryPath, fileName);
                                Log.Info("Importing search configuration in " + fileName);
                                searchMan.ImportSearchConfiguration(context, pathToSearchSettingsFile);
                            }
                        }
                        else
                        {
                            ImportAllSearchSettings(context);
                        }
                        break;
                    }
                    case  InstallationOperation.DeleteSites:
                    {
                        TeardownSites();
                        break;
                    }
                    case InstallationOperation.DeleteFieldsAndContentTypes:
                    {
                        if (useConfigurationForInstall)
                        {
                            foreach (
                                var fileName in
                                    siteSetupManagerFromConfig.ConfigurationSiteCollection.ContentTypeConfigurations)
                            {
                                var filePath = Path.Combine(ConfigurationDirectoryPath, fileName);
                                DeleteContentTypesSpecifiedInFile(context, filePath);
                            }
                            foreach (
                                var fileName in
                                    siteSetupManagerFromConfig.ConfigurationSiteCollection.FieldConfigurations)
                            {
                                var filePath = Path.Combine(ConfigurationDirectoryPath, fileName);
                                DeleteFieldsSpecifiedInFile(context, filePath);
                            }
                        }
                        else
                        {
                            DeleteAllSherpaSiteColumnsAndContentTypes(context);
                        }
                        break;
                    }
                    case InstallationOperation.ExportTaxonomy:
                    {
                        ExportTaxonomyGroup();
                        break;
                    }
                    case InstallationOperation.ForceRecrawl:
                    {
                        ForceReCrawl();
                        break;
                    }
                    case InstallationOperation.ExitApplication:
                    {
                        Environment.Exit(1);
                        break;
                    }
                    default:
                    {
                        Log.Warn("Operation not supported in unmanaged mode");
                        break;
                    }
                }
            }
            Log.Debug("Completed installation operation");
        }

        private void InstallAllTaxonomy(ClientContext context)
        {
            foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*taxonomy.json", SearchOption.AllDirectories))
            {
                InstallTaxonomyFromSingleFile(context, file);
            }
        }

        private void InstallTaxonomyFromSingleFile(ClientContext context, string pathToFile)
        {
            Log.Info("Starting installation of taxonomy based on " + pathToFile);
            var taxPersistanceProvider = new FilePersistanceProvider<ShTermGroup>(pathToFile);
            var taxonomyManager = new TaxonomyManager(taxPersistanceProvider.Load());
            taxonomyManager.WriteTaxonomyToTermStore(context);
        }

        private void ExportTaxonomyGroup()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Please provide name of the taxonomy term group to export: ");
            Console.ResetColor();
            var input = Console.ReadLine();
            ExportTaxonomyGroup(input);
        }

        private void ExportTaxonomyGroup(string groupName)
        {
            Log.Info("Starting export of taxonomy group " + groupName);
            using (var context = new ClientContext(_urlToSite))
            {
                context.Credentials = _credentials;
                var outputDirectoryPath = Path.Combine(_rootPath, "export");
                Directory.CreateDirectory(outputDirectoryPath);
                var taxPersistanceProvider = new FilePersistanceProvider<ShTermGroup>(Path.Combine(outputDirectoryPath, groupName.ToLower().Replace(" ", "") + "taxonomy.json"));
                var taxonomyManager = new TaxonomyManager();
                var groupConfig = taxonomyManager.ExportTaxonomyGroupToConfig(context, groupName);
                if (groupConfig != null)
                {
                    taxPersistanceProvider.Save(groupConfig);
                    Log.Info("Completed export of taxonomy group " + groupName);
                }
            }
        }
        private void UploadAndActivateAllSandboxSolutions(ClientContext context)
        {
            Log.Info("Uploading and activating sandboxed solution(s)");
            var deployManager = new DeployManager(_urlToSite, _credentials, _isSharePointOnline);
            var solutionPackages = Directory.GetFiles(SolutionsDirectoryPath, "*.wsp", SearchOption.AllDirectories);

            if (!deployManager.IsCurrentUserSiteCollectionAdmin(context))
            {
                Log.Fatal("You need to be site collection administrator to perform this operation.");
                return;
            }
            foreach (var file in solutionPackages)
            {
                UploadAndActivatePackage(context, deployManager, file);
            }
            
            Log.Info("Done uploading and activating sandboxed solution(s)");
        }

        private static void UploadAndActivatePackage(ClientContext context, DeployManager deployManager, string file)
        {
            Log.Info("Processing solution package " + file);
            deployManager.UploadDesignPackageToSiteAssets(context, file);
            deployManager.ActivateDesignPackage(context, file, "SiteAssets");
        }

        private void CreateAllSiteColumnsAndContentTypes(ClientContext context)
        {
            ConfigureSitesFromAllSiteHierarchyFiles(context, true);
            Log.Info("Starting setup of site columns and content types");

            foreach (var filePath in Directory.GetFiles(ConfigurationDirectoryPath, "*fields.json", SearchOption.AllDirectories))
            {
                CreateFieldsFromFile(context, filePath);
            }
            foreach (var filePath in Directory.GetFiles(ConfigurationDirectoryPath, "*contenttypes.json", SearchOption.AllDirectories))
            {
                CreateContentTypesFromFile(context, filePath);
            }
            
            Log.Info("Done setup of site columns and content types");
        }

        private static void CreateContentTypesFromFile(ClientContext context, string filePath)
        {
            var contentTypePersister = new FilePersistanceProvider<List<ShContentType>>(filePath);
            var contentTypeManager = new ContentTypeManager(context, contentTypePersister.Load());
            contentTypeManager.CreateContentTypes();
        }

        private static void CreateFieldsFromFile(ClientContext context, string filePath)
        {
            var fieldPersister = new FilePersistanceProvider<List<ShField>>(filePath);
            var fieldManager = new FieldManager(context, fieldPersister.Load());
            fieldManager.CreateFields();
        }

        private void ConfigureSitesFromAllSiteHierarchyFiles(ClientContext context)
        {
            ConfigureSitesFromAllSiteHierarchyFiles(context, false);
        }

        private void ConfigureSitesFromAllSiteHierarchyFiles(ClientContext context, bool onlyContentTypeDependecyFeatures)
        {
            Log.Debug("Starting ConfigureSitesFromAllSiteHierarchyFiles, only content type dependencies: " + onlyContentTypeDependecyFeatures);

            foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*sitehierarchy.json", SearchOption.AllDirectories))
            {
                var sitePersister = new FilePersistanceProvider<ShSiteCollection>(file);
                var siteManager = new SiteSetupManager(context, sitePersister.Load());
                if (onlyContentTypeDependecyFeatures)
                {
                    Log.Debug("ConfigureSitesFromAllSiteHierarchyFiles: Activating only content type dependecy features");
                    siteManager.ActivateContentTypeDependencyFeatures();
                }
                else
                {
                    Log.Debug("ConfigureSitesFromAllSiteHierarchyFiles: Setting up sites in normal mode");
                    siteManager.SetupSites();
                }
            }
            
        }

        private void ImportAllSearchSettings(ClientContext context)
        {
            var searchMan = new SearchImportManager();
            var pathToSearchXmls = Directory.GetFiles(SearchDirectoryPath);
            foreach (var pathToSearchXml in pathToSearchXmls)
            {
                Log.Info("Importing search setting " + pathToSearchXml);
                searchMan.ImportSearchConfiguration(context, pathToSearchXml);
            }
        }

        private void TeardownSites()
        {
            using (var context = new ClientContext(_urlToSite) { Credentials = _credentials })
            {
                Log.Info("Deleting sites");
                SiteSetupManager.DeleteSites(context);
            }
        }

        private void DeleteAllSherpaSiteColumnsAndContentTypes(ClientContext context)
        {
            Log.Info("Deleting all custom site columns and content types");
            foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*contenttypes.json", SearchOption.AllDirectories))
            {
                DeleteContentTypesSpecifiedInFile(context, file);
            }
            foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*fields.json", SearchOption.AllDirectories))
            {
                DeleteFieldsSpecifiedInFile(context, file);
            }
            Log.Info("Done deleting all custom site columns and content types");
        }

        private static void DeleteFieldsSpecifiedInFile(ClientContext context, string file)
        {
            Log.Info("Deleting all fields with the same group as the ones in the file " + file);
            var siteColumnPersister = new FilePersistanceProvider<List<ShField>>(file);
            var siteColumnManager = new FieldManager(context, siteColumnPersister.Load());
            siteColumnManager.DeleteAllCustomFields();
        }

        private static void DeleteContentTypesSpecifiedInFile(ClientContext context, string file)
        {
            Log.Info("Deleting all content types with the same group as the ones in the file " + file);
            var contentTypePersister = new FilePersistanceProvider<List<ShContentType>>(file);
            var contentTypeManager = new ContentTypeManager(context, contentTypePersister.Load());
            contentTypeManager.DeleteAllCustomContentTypes();
        }

        private void ForceReCrawl()
        {
            Log.Info("(Hidden feature) Forcing recrawl of rootsite and all subsites");
            var deployManager = new DeployManager(_urlToSite, _credentials, _isSharePointOnline);
            deployManager.ForceRecrawl();
        }

        public InstallationOperation GetInstallationOperationFromInput(string input)
        {
            int inputNum;
            if (!int.TryParse(input, out inputNum))
            {
                return InstallationOperation.Invalid;
            }
            switch (inputNum)
            {
                case 1:
                {
                    return InstallationOperation.InstallTaxonomy;
                }
                case 2:
                {
                    return InstallationOperation.UploadAndActivateSolution;
                }
                case 3:
                {
                    return InstallationOperation.InstallFieldsAndContentTypes;
                }
                case 4:
                {
                    return InstallationOperation.ConfigureSites;
                }
                case 5:
                {
                    return InstallationOperation.ImportSearch;
                }
                case 6:
                {
                    return InstallationOperation.ExportTaxonomy;
                }
                case 7:
                {
                    return InstallationOperation.ExecuteCustomTasks;
                }
                case 8:
                {
                    return InstallationOperation.DeleteSites;
                }
                case 9:
                {
                    return InstallationOperation.DeleteFieldsAndContentTypes;
                }
                case 1337:
                {
                    return InstallationOperation.ForceRecrawl;
                }
                case 0:
                {
                    return InstallationOperation.ExitApplication;
                }
                default:
                {
                    return InstallationOperation.Invalid;
                }
            }
        }
    }
}
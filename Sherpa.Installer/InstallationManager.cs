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
        private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
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

            _log.DebugFormat("Site Url: \t{0}\r\nConfigpath: \t{1}\r\nSPO: \t\t{2}", _urlToSite.AbsoluteUri, _rootPath, _isSharePointOnline);
        }


        public void InstallUnmanaged(string siteHierarchyFileName, string operationInput)
        {
            _log.Debug("Starting unmanaged installation");
            if (string.IsNullOrEmpty(siteHierarchyFileName))
            {
                _log.Fatal("Configuration filepath is empty - cannot continue");
                return;
            }
            if (string.IsNullOrEmpty(operationInput))
            {
                _log.Fatal("Operations is empty - cannot continue");
                return;
            }
            Console.WriteLine("Starting unmanaged installation");
            using (var context = new ClientContext(_urlToSite) {Credentials = _credentials})
            {
                var installationOperation = GetInstallationOperationFromInput(operationInput);
                var configurationFile = Path.Combine(ConfigurationDirectoryPath, siteHierarchyFileName);
                if (!File.Exists(configurationFile))
                {
                    _log.Fatal("Couldn't find the configuration file " + configurationFile);
                    return;
                }
                var sitePersister = new FilePersistanceProvider<ShSiteCollection>(configurationFile);
                var siteManager = new SiteSetupManager(context, sitePersister.Load());

                switch (installationOperation)
                {
                    case InstallationOperation.InstallTaxonomy:
                    {
                        foreach (var filename in siteManager.ConfigurationSiteCollection.TaxonomyConfigurations)
                        {
                            InstallTaxonomyFromSingleFile(context, Path.Combine(ConfigurationDirectoryPath, filename));
                        }
                        break;
                    }
                    case InstallationOperation.UploadAndActivateSolution:
                    {
                        var deployManager = new DeployManager(_urlToSite, _credentials, _isSharePointOnline);
                        foreach (var filename in siteManager.ConfigurationSiteCollection.SandboxedSolutions)
                        {
                            UploadAndActivatePackage(context, deployManager, Path.Combine(SolutionsDirectoryPath, filename));
                        }
                        break;
                    }
                    case InstallationOperation.InstallFieldsAndContentTypes:
                    {
                        siteManager.ActivateContentTypeDependencyFeatures();
                        foreach (var fileName in siteManager.ConfigurationSiteCollection.FieldConfigurations)
                        {
                            var filePath = Path.Combine(ConfigurationDirectoryPath, fileName);
                            CreateFieldsFromFile(context, filePath);
                        }
                        foreach (var fileName in siteManager.ConfigurationSiteCollection.ContentTypeConfigurations)
                        {
                            var filePath = Path.Combine(ConfigurationDirectoryPath, fileName);
                            CreateContentTypesFromFile(context, filePath);
                        }
                        break;
                    }
                    case InstallationOperation.ConfigureSites:
                    {
                        siteManager.SetupSites();
                        break;
                    }
                    case InstallationOperation.ImportSearch:
                    {
                        var searchMan = new SearchImportManager();
                        foreach (var fileName in siteManager.ConfigurationSiteCollection.SearchConfigurations)
                        {
                            var pathToSearchSettingsFile = Path.Combine(SearchDirectoryPath, fileName);
                            searchMan.ImportSearchConfiguration(context, pathToSearchSettingsFile);
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
                        foreach (var fileName in siteManager.ConfigurationSiteCollection.ContentTypeConfigurations)
                        {
                            var filePath = Path.Combine(ConfigurationDirectoryPath, fileName);
                            DeleteContentTypesSpecifiedInFile(context, filePath);
                        }
                        foreach (var fileName in siteManager.ConfigurationSiteCollection.FieldConfigurations)
                        {
                            var filePath = Path.Combine(ConfigurationDirectoryPath, fileName);
                            DeleteFieldsSpecifiedInFile(context, filePath);
                        }
                        break;
                    }
                    default:
                    {
                        _log.Warn("Operation not supported in unmanaged mode");
                        break;
                    }
                }
            }
            _log.Debug("Completed unmanaged installation");
        }

        public void SetupTaxonomy()
        {
            using (var context = new ClientContext(_urlToSite))
            {
                context.Credentials = _credentials;
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*taxonomy.json", SearchOption.AllDirectories))
                {
                    InstallTaxonomyFromSingleFile(context, file);
                }
            }
        }

        private void InstallTaxonomyFromSingleFile(ClientContext context, string pathToFile)
        {
            _log.Info("Starting installation of taxonomy based on " + pathToFile);
            var taxPersistanceProvider = new FilePersistanceProvider<ShTermGroup>(pathToFile);
            var taxonomyManager = new TaxonomyManager(taxPersistanceProvider.Load());
            taxonomyManager.WriteTaxonomyToTermStore(context);
        }

        public void ExportTaxonomyGroup()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Please provide name of the taxonomy term group to export: ");
            Console.ResetColor();
            var input = Console.ReadLine();
            ExportTaxonomyGroup(input);
        }

        public void ExportTaxonomyGroup(string groupName)
        {
            _log.Info("Starting export of taxonomy group " + groupName);
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
                    _log.Info("Completed export of taxonomy group " + groupName);
                }
            }
        }
        public void UploadAndActivateSandboxSolutions()
        {
            Console.WriteLine("Uploading and activating sandboxed solution(s)");
            var deployManager = new DeployManager(_urlToSite, _credentials, _isSharePointOnline);
            var solutionPackages = Directory.GetFiles(SolutionsDirectoryPath, "*.wsp", SearchOption.AllDirectories);

            using (var context = new ClientContext(_urlToSite))
            {
                context.Credentials = _credentials;

                if (!deployManager.IsCurrentUserSiteCollectionAdmin(context))
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("You need to be site collection administrator to perform this operation.");
                    Console.ResetColor();
                }
                foreach (var file in solutionPackages)
                {
                    UploadAndActivatePackage(context, deployManager, file);
                }
            }
            Console.WriteLine("Done uploading and activating sandboxed solution(s)");
        }

        private static void UploadAndActivatePackage(ClientContext context, DeployManager deployManager, string file)
        {
            _log.Debug("Processing solution package " + file);
            deployManager.UploadDesignPackageToSiteAssets(context, file);
            deployManager.ActivateDesignPackage(context, file, "SiteAssets");
        }

        public void CreateSiteColumnsAndContentTypes()
        {
            ConfigureSites(true);
            Console.WriteLine("Starting setup of site columns and content types");

            using (var context = new ClientContext(_urlToSite))
            {
                context.Credentials = _credentials;
                foreach (var filePath in Directory.GetFiles(ConfigurationDirectoryPath, "*fields.json", SearchOption.AllDirectories))
                {
                    CreateFieldsFromFile(context, filePath);
                }
                foreach (var filePath in Directory.GetFiles(ConfigurationDirectoryPath, "*contenttypes.json", SearchOption.AllDirectories))
                {
                    CreateContentTypesFromFile(context, filePath);
                }
            }
            Console.WriteLine("Done setup of site columns and content types");
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

        public void ConfigureSites()
        {
            ConfigureSites(false);
        }

        public void ConfigureSites(bool onlyContentTypeDependecyFeatures)
        {
            _log.Debug("Starting ConfigureSites, only content type dependencies: " + onlyContentTypeDependecyFeatures);
            using (var context = new ClientContext(_urlToSite) { Credentials = _credentials })
            {
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*sitehierarchy.json", SearchOption.AllDirectories))
                {
                    var sitePersister = new FilePersistanceProvider<ShSiteCollection>(file);
                    var siteManager = new SiteSetupManager(context, sitePersister.Load());
                    if (onlyContentTypeDependecyFeatures)
                    {
                        _log.Debug("ConfigureSites: Activating only content type dependecy features");
                        siteManager.ActivateContentTypeDependencyFeatures();
                    }
                    else
                    {
                        _log.Debug("ConfigureSites: Setting up sites in normal mode");
                        siteManager.SetupSites();
                    }
                }
            }
        }

        public void ImportSearchSettings()
        {
            using (var context = new ClientContext(_urlToSite) { Credentials = _credentials })
            {
                var searchMan = new SearchImportManager();
                var pathToSearchXmls = Directory.GetFiles(SearchDirectoryPath);
                foreach (var pathToSearchXml in pathToSearchXmls)
                {
                    _log.Info("Importing search setting " + pathToSearchXml);
                    searchMan.ImportSearchConfiguration(context, pathToSearchXml);
                }
            }
        }

        public void TeardownSites()
        {
            using (var context = new ClientContext(_urlToSite) { Credentials = _credentials })
            {
                _log.Info("Deleting sites");
                SiteSetupManager.DeleteSites(context);
            }
        }

        public void DeleteAllSherpaSiteColumnsAndContentTypes()
        {
            Console.WriteLine("Deleting all custom site columns and content types");
            using (var context = new ClientContext(_urlToSite))
            {
                context.Credentials = _credentials;
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*contenttypes.json", SearchOption.AllDirectories))
                {
                    DeleteContentTypesSpecifiedInFile(context, file);
                }
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*fields.json", SearchOption.AllDirectories))
                {
                    DeleteFieldsSpecifiedInFile(context, file);
                }
            }
            Console.WriteLine("Done deleting all custom site columns and content types");
        }

        private static void DeleteFieldsSpecifiedInFile(ClientContext context, string file)
        {
            var siteColumnPersister = new FilePersistanceProvider<List<ShField>>(file);
            var siteColumnManager = new FieldManager(context, siteColumnPersister.Load());
            siteColumnManager.DeleteAllCustomFields();
        }

        private static void DeleteContentTypesSpecifiedInFile(ClientContext context, string file)
        {
            var contentTypePersister = new FilePersistanceProvider<List<ShContentType>>(file);
            var contentTypeManager = new ContentTypeManager(context, contentTypePersister.Load());
            contentTypeManager.DeleteAllCustomContentTypes();
        }

        public void ForceReCrawl()
        {
            Console.WriteLine("(Hidden feature) Forcing recrawl of rootsite and all subsites");
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

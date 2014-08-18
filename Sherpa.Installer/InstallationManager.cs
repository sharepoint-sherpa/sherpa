using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Microsoft.SharePoint.Client;
using Sherpa.Library;
using Sherpa.Library.ContentTypes;
using Sherpa.Library.ContentTypes.Model;
using Sherpa.Library.Deploy;
using Sherpa.Library.SiteHierarchy;
using Sherpa.Library.SiteHierarchy.Model;
using Sherpa.Library.Taxonomy;
using Sherpa.Library.Taxonomy.Model;

namespace Sherpa.Installer
{
    class InstallationManager
    {
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

        public InstallationManager(Uri urlToSite, ICredentials credentials, bool isSharePointOnline, string rootPath)
        {
            _urlToSite = urlToSite;
            _credentials = credentials;
            _isSharePointOnline = isSharePointOnline;
            _rootPath = rootPath ?? Environment.CurrentDirectory;
        }

        public void SetupTaxonomy()
        {
            Console.WriteLine("Starting installation of term groups, term sets and terms");
            using (var context = new ClientContext(_urlToSite))
            {
                context.Credentials = _credentials;
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*taxonomy.json", SearchOption.AllDirectories))
                {
                    var taxPersistanceProvider = new FilePersistanceProvider<GtTermSetGroup>(file);
                    var taxonomyManager = new TaxonomyManager(taxPersistanceProvider.Load());
                    taxonomyManager.WriteTaxonomyToTermStore(context);
                }
            }
            Console.WriteLine("Done installation of term groups, term sets and terms");
        }

        public void UploadAndActivateSandboxSolution()
        {
            Console.WriteLine("Uploading and activating sandboxed solution(s)");
            var deployManager = new DeployManager(_urlToSite, _credentials, _isSharePointOnline);
            foreach (var file in Directory.GetFiles(SolutionsDirectoryPath, "*.wsp", SearchOption.AllDirectories))
            {
                deployManager.UploadDesignPackageToSiteAssets(file);
                deployManager.ActivateDesignPackage(file, "SiteAssets");
            }
            Console.WriteLine("Done uploading and activating sandboxed solution(s)");
        }

        public void CreateSiteColumnsAndContentTypes()
        {
            Console.WriteLine("Starting setup of site columns and content types");
            using (var context = new ClientContext(_urlToSite))
            {
                context.Credentials = _credentials;
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*fields.json", SearchOption.AllDirectories))
                {
                    var siteColumnPersister = new FilePersistanceProvider<List<GtField>>(file);
                    var siteColumnManager = new FieldManager(context, siteColumnPersister.Load());
                    siteColumnManager.CreateSiteColumns();
                }
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*contenttypes.json", SearchOption.AllDirectories))
                {
                    var contentTypePersister = new FilePersistanceProvider<List<GtContentType>>(file);
                    var contentTypeManager = new ContentTypeManager(context, contentTypePersister.Load());
                    contentTypeManager.CreateContentTypes();
                }
            }
            Console.WriteLine("Done setup of site columns and content types");
        }

        public void ConfigureSites()
        {
            Console.WriteLine("Starting configuring sites");
            using (var clientContext = new ClientContext(_urlToSite) { Credentials = _credentials })
            {
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*sitehierarchy.json", SearchOption.AllDirectories))
                {
                    var sitePersister = new FilePersistanceProvider<GtWeb>(file);
                    var siteManager = new SiteSetupManager(clientContext, sitePersister.Load());
                    siteManager.SetupSites();
                }
            }
            Console.WriteLine("Done configuring sites");
        }

        public void TeardownSites()
        {
            Console.WriteLine("Starting teardown of sites");
            using (var clientContext = new ClientContext(_urlToSite) { Credentials = _credentials })
            {
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*sitehierarchy.json", SearchOption.AllDirectories))
                {
                    var sitePersister = new FilePersistanceProvider<GtWeb>(file);
                    var siteManager = new SiteSetupManager(clientContext, sitePersister.Load());
                    siteManager.DeleteSites();
                }
            }
            Console.WriteLine("Done teardown of sites");
        }

        public void DeleteAllSherpaSiteColumnsAndContentTypes()
        {
            Console.WriteLine("Deleting all Glitterind columns and content types");
            using (var context = new ClientContext(_urlToSite))
            {
                context.Credentials = _credentials;
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*contenttypes.json", SearchOption.AllDirectories))
                {
                    var contentTypePersister = new FilePersistanceProvider<List<GtContentType>>(file);
                    var contentTypeManager = new ContentTypeManager(context, contentTypePersister.Load());
                    contentTypeManager.DeleteAllCustomContentTypes();
                }
                foreach (var file in Directory.GetFiles(ConfigurationDirectoryPath, "*fields.json", SearchOption.AllDirectories))
                {
                    var siteColumnPersister = new FilePersistanceProvider<List<GtField>>(file);
                    var siteColumnManager = new FieldManager(context, siteColumnPersister.Load());
                    siteColumnManager.DeleteAllCustomFields();
                }
            }
            Console.WriteLine("Done deleting all Glitterind columns and content types");
        }

        public void ForceReCrawl()
        {
            var deployManager = new DeployManager(_urlToSite, _credentials, _isSharePointOnline);
            deployManager.ForceRecrawl();
        }
    }
}

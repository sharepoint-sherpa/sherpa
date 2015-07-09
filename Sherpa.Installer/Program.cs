﻿using System;
using System.Linq;
using System.Net;
using System.Reflection;
using log4net;
using log4net.Config;
using Sherpa.Library;
using Sherpa.Installer.Model;
using System.IO;
using System.Collections.Generic;

namespace Sherpa.Installer
{
    class Program
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static ICredentials Credentials { get; set; }
        public static InstallationManager InstallationManager { get; set; }
        private static Options ProgramOptions { get; set; }
        private static Uri UrlToSite { get; set; }
        private static bool Unmanaged { get; set; }

        static int Main(string[] args)
        {
            XmlConfigurator.Configure(); //Initialize log4net
            Log.Debug("Sherpa application started");


            try
            {
                ProgramOptions = OptionsParser.ParseArguments(args);
                if (!string.IsNullOrEmpty(ProgramOptions.Setup))
                {
                    try
                    {
                        var filePath = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(System.IO.Directory.GetCurrentDirectory())), "Environments.json");
                        var setupPersister = new FilePersistanceProvider<List<ShEnvironment>>(filePath);
                        List<ShEnvironment> environments = setupPersister.Load();
                        var env = environments.FirstOrDefault(e => e.Name == ProgramOptions.Setup);
                        ProgramOptions.UrlToSite = env.UrlToSite;
                        ProgramOptions.UserName = env.UserName;
                        ProgramOptions.RootPath = env.RootPath;
                        ProgramOptions.SharePointOnline = env.SharePointOnline;
                        ProgramOptions.SiteHierarchy = env.SiteHierarchy;
                    } catch(Exception) {
                        Log.Fatal("The supplied setup options are not valid.");
                        Environment.Exit(1);
                    }
                }
                UrlToSite = new Uri(ProgramOptions.UrlToSite);
                Unmanaged = !string.IsNullOrEmpty(ProgramOptions.Operations);

                Log.Debug(string.Format("Sherpa started with the following options - URL: {0}, userName: {1}, configPath: {2}, spo: {3}, unmanaged: {4}", 
                    ProgramOptions.UrlToSite,
                    ProgramOptions.UserName,
                    ProgramOptions.RootPath,
                    ProgramOptions.SharePointOnline,
                    Unmanaged
                ));
            }
            catch (Exception e)
            {
                Log.Fatal("Invalid parameters, application cannot continue");
                Environment.Exit(1);
            }
            if (!Unmanaged)
            {
                PrintLogo();
            }
            else
            {
                Log.Info("Sherpa initialized in unattended mode");
            }
            if (!IsCorrectSharePointAssemblyVersionLoaded())
            {
                Log.Fatal("Old version of SharePoint assemblies loaded. Application cannot be run on a machine with SharePoint 2010 or older installed. Content type installation only works with SharePoint 2013 SP1 and later.");
                Environment.Exit(1);
            }
            try
            {
                var authManager = new AuthenticationHandler();
                Credentials = authManager.GetSessionAuthCredentials(ProgramOptions.SharePointOnline, ProgramOptions.UserName, UrlToSite);

                RunApplication();
                Log.Debug("Application exiting");
            }
            catch (Exception exception)
            {
                Log.Fatal("An exception occured: " + exception.Message);
                Log.Debug(exception.StackTrace);
                if (Unmanaged) return 1;
                RunApplication();
            }
            return 0;
        }

        private static void RunApplication()
        {
            InstallationManager = new InstallationManager(UrlToSite, Credentials, ProgramOptions.SharePointOnline, ProgramOptions.RootPath);

            if (!Unmanaged) ShowStartScreenAndExecuteCommand();
            else
            {
                var operation = InstallationManager.GetInstallationOperationFromInput(ProgramOptions.Operations);
                InstallationManager.InstallOperation(operation, ProgramOptions.SiteHierarchy);
            }
        }

        private static void ShowStartScreenAndExecuteCommand()
        {
            Console.WriteLine("Configuring {0}", UrlToSite);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("# Available application operations #");
            Console.ResetColor();
            Console.WriteLine("Press 1 to install managed metadata groups and term sets");
            Console.WriteLine("Press 2 to upload and activate sandboxed solution");
            Console.WriteLine("Press 3 to setup site columns and content types");
            Console.WriteLine("Press 4 to setup sites and features");
            Console.WriteLine("Press 5 to import search configurations");
            Console.WriteLine("Press 6 to export taxonomy group");
            Console.WriteLine("Press 7 to execute custom tasks");
            Console.WriteLine("Press 8 to DELETE all sites (except root site)");
            Console.WriteLine("Press 9 to DELETE all custom site columns and content types");
            Console.WriteLine("Press 0 to exit application");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Select a number to perform an operation: ");
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            var input = Console.ReadLine();
            Console.ResetColor();
            HandleCommandKeyPress(input);
        }

        private static void HandleCommandKeyPress(string input)
        {
            var operation = InstallationManager.GetInstallationOperationFromInput(input);
            InstallationManager.InstallOperation(operation, ProgramOptions.SiteHierarchy);
            ShowStartScreenAndExecuteCommand();
        }

        private static bool IsCorrectSharePointAssemblyVersionLoaded()
        {
            var sharePointAssembly = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Single(a => a.Name.Equals("Microsoft.SharePoint.Client"));

            return sharePointAssembly.Version.Major >= 15;
        }

        private static void PrintLogo()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"     _______. __    __   _______ .______   .______     ___      ");
            Console.WriteLine(@"    /       ||  |  |  | |   ____||   _  \  |   _  \   /   \     ");
            Console.WriteLine(@"   |   (----`|  |__|  | |  |__   |  |_)  | |  |_)  | /  ^  \    ");
            Console.WriteLine(@"    \   \    |   __   | |   __|  |      /  |   ___/ /  /_\  \   ");
            Console.WriteLine(@".----)   |   |  |  |  | |  |____ |  |\  \_ |  |    /   ___   \  ");
            Console.WriteLine(@"|_______/    |__|  |__| |_______|| _| `.__|| _|   /__/     \__\ ");
            Console.WriteLine(@"                                                                ");
            Console.ResetColor();
        }
    }
}

using System;
using System.Net;
using Microsoft.SharePoint.Client;

namespace Sherpa.Installer
{
    class Program
    {
        public static ICredentials Credentials { get; set; }
        public static InstallationManager InstallationManager { get; set; }
        private static Options ProgramOptions { get; set; }
        private static Uri UrlToSite { get; set; }

        static void Main(string[] args)
        {
            PrintLogo();
            try
            {
                ProgramOptions = OptionsParser.ParseArguments(args);
                UrlToSite = new Uri(ProgramOptions.UrlToSite);
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid parameters, application cannot continue");
                Environment.Exit(1);
            }
            PrintLogo();

            if (ProgramOptions.SharePointOnline)
            {
                Console.WriteLine("Login with your password to {0}", ProgramOptions.UrlToSite);
                var authenticationHandler = new AuthenticationHandler();
                Credentials = authenticationHandler.GetCredentialsForSharePointOnline(ProgramOptions.UserName, UrlToSite);
            }
            else
            {
                Credentials = CredentialCache.DefaultCredentials;

                using (new ClientContext(ProgramOptions.UrlToSite) { Credentials = Credentials })
                {
                    Console.WriteLine("Authenticated with default credentials");
                }
            }
            RunApplication();
        }

        private static void RunApplication()
        {
            try
            {
                InstallationManager = new InstallationManager(UrlToSite, Credentials, ProgramOptions.SharePointOnline, ProgramOptions.RootPath);
                ShowStartScreenAndExecuteCommand();
            }
            catch (Exception exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An exception occured: " + exception.Message);
                Console.WriteLine(exception.StackTrace);
                Console.ForegroundColor = ConsoleColor.White;
                RunApplication();
            }
        }

        private static void ShowStartScreenAndExecuteCommand()
        {
            Console.WriteLine("Application options");
            Console.WriteLine("Press 1 to install managed metadata groups and term sets.");
            Console.WriteLine("Press 2 to upload and activate sandboxed solution.");
            Console.WriteLine("Press 3 to setup site columns and content types.");
            Console.WriteLine("Press 4 to setup sites and features.");
            Console.WriteLine("Press 8 to DELETE all sites (except root site).");
            Console.WriteLine("Press 9 to DELETE all custom site columns and content types.");
            Console.WriteLine("Press 0 to exit application.");
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
            int inputNum;
            if (!int.TryParse(input, out inputNum))
            {
                Console.WriteLine("Invalid input");
                ShowStartScreenAndExecuteCommand();
            }
            switch (inputNum)
            {
                case 1:
                {
                    InstallationManager.SetupTaxonomy();
                    break;
                }
                case 2:
                {
                    InstallationManager.UploadAndActivateSandboxSolution();
                    break;
                }
                case 3:
                {
                    InstallationManager.CreateSiteColumnsAndContentTypes();
                    break;
                }
                case 4:
                {
                    InstallationManager.ConfigureSites();
                    break;
                }
                case 8:
                {
                    InstallationManager.TeardownSites();
                    break;
                }
                case 9:
                {
                    InstallationManager.DeleteAllSherpaSiteColumnsAndContentTypes();
                    break;
                }
                case 1337:
                {
                    Console.WriteLine("(Hidden feature) Forcing recrawl of rootsite and all subsites");
                    InstallationManager.ForceReCrawl();
                    break;
                }
                case 0:
                {
                    Environment.Exit(0);
                    break;
                }
                default:
                {
                    Console.WriteLine("Invalid input");
                    break;
                }
            }
            ShowStartScreenAndExecuteCommand();
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
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}

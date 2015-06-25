using System;
using System.Reflection;
using CommandLine;
using CommandLine.Text;
using log4net;

namespace Sherpa.Installer
{
    public static class OptionsParser
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public static Options ParseArguments(string[] args)
        {
            var options = new Options();
            if (args.Length == 0)
            {
                Log.Info("You can start Sherpa from the command line to specify multiple arguments.");

                Uri uriInput = null;
                while (uriInput == null)
                {
                    try
                    {
                        uriInput = new Uri(ReadArgument("Specify URL of site collection where the solution(s) should be installed: "));
                        options.UrlToSite = uriInput.AbsoluteUri;
                    }
                    catch{}
                }

                bool? spoInput = null;
                while (spoInput == null)
                {
                    var input = ReadArgument("Is this SharePoint Online?").ToLower();
                    if (input == "ja" || input == "yes" || input == "1" || input == "true") spoInput = true;
                    if (input == "nei" || input == "no" || input == "0" || input == "false") spoInput = false;
                }
                options.SharePointOnline = spoInput.Value;

                if (spoInput.Value)
                {
                    options.UserName = ReadArgument("Specify site collection administrator username: ");
                }
            }
            else if (Parser.Default.ParseArguments(args, options))
            {
                Log.Debug("Parsing arguments from the command line");
            }

            return options;
        }

        public static string ReadArgument(string argumentDescription)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(argumentDescription);
            Console.ForegroundColor = ConsoleColor.White;
            return Console.ReadLine();
        }
    }

    public sealed class Options
    {
        [ParserState]
        public IParserState LastParserState { get; set; }

        [Option("url", Required = true, HelpText = "Full URL to the target SharePoint site collection")]
        public string UrlToSite { get; set; }

        [Option('u', "userName", HelpText = "Username@domain whos credentials will be used during installation (spo only)")]
        public string UserName { get; set; }

        [Option("spo", HelpText = "Specify if the solution is targeting SharePoint Online")]
        public bool SharePointOnline { get; set; }

        [Option("path", HelpText = "Path to directory where the config and solutions folders are present. Not specifying will use application directory")]
        public string RootPath { get; set; }

        [Option("op", HelpText = "For unmanaged execution, specify the operation you want to execute by referencing the operation's ID. 1 is taxonomy, 2 is sandbox solution upload and so forth.")]
        public string Operations { get; set; }

        [Option("conf", HelpText = "For unmanaged execution, specify the file name of the main site hierarchy configuration file. The file must reside in the config folder.")]
        public string SiteHierarchy { get; set; }

        [Option('v', "verbose", HelpText = "Write everything to console")]
        public bool Verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              current => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}

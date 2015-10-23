using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net;
using Sherpa.Library.SiteHierarchy;

namespace Sherpa.Library
{
    public class FileListenerAndUploader
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        public SiteSetupManager SetupManager { get; set; }
        public ManualResetEvent ResetEvent { get; set; }
        public List<string> ChangedFilesInIteration { get; set; }

        public void CreateFileWatcher(string path, SiteSetupManager setupManager)
        {
            SetupManager = setupManager;

            FileSystemWatcher watcher = new FileSystemWatcher();
            ResetEvent = new ManualResetEvent(false);

            watcher.Path = path;
            /* Watch for changes in LastAccess and LastWrite times, and 
               the renaming of files or directories. */
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
               | NotifyFilters.FileName | NotifyFilters.DirectoryName;

            watcher.IncludeSubdirectories = true;

            // Add event handlers.
            watcher.Changed += new FileSystemEventHandler(FileChange);
            watcher.Created += new FileSystemEventHandler(FileChange);

            // Begin watching.
            watcher.EnableRaisingEvents = true;

            ChangedFilesInIteration = new List<string>();
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Watching for changes to files in Sherpa content directory...");
                Console.ResetColor();

                if (ResetEvent.WaitOne())
                {
                    SetupManager.UploadChangedFiles();
                    ChangedFilesInIteration.Clear();
                    ResetEvent.Reset();
                }
                else
                {
                    Console.WriteLine("Waiting timed-out");
                }
            }
            //watcher.WaitForChanged(WatcherChangeTypes.Changed | WatcherChangeTypes.Created | WatcherChangeTypes.Renamed, -1);
        }

        // Define the event handlers.
        private void FileChange(object source, FileSystemEventArgs e)
        {
            //Special handling when some files are not touched themselves, but have an ~something.tmp appended
            var changedFile = e.Name.Split('~')[0];
            if (changedFile.EndsWith(".js") || changedFile.EndsWith(".css") || changedFile.EndsWith(".txt") ||
                changedFile.EndsWith(".html") || changedFile.EndsWith(".webpart") || changedFile.EndsWith(".png"))
            {
                if (!ChangedFilesInIteration.Contains(changedFile))
                {
                    Log.InfoFormat("File {0} changed and a new upload is starting", changedFile);
                    ChangedFilesInIteration.Add(changedFile);
                    ResetEvent.Set();
                }
            }
        }
    }
}

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShContentFolder
    {
        public string FolderName { get; set; }
        public string FolderUrl { get; set; }
        public string ListName { get; set; }
        public string PropertiesFile { get; set; }
        /* Example: '.ts,.less,.tiff,.ps' */
        public string ExcludeExtensions { get; set; }
    }
}

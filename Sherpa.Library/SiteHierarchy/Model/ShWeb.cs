using System.Collections.Generic;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShWeb
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Template { get; set; }
        public string Description { get; set; }
        public string WelcomePageUrl { get; set; }
        public string AlternateCssUrl { get; set; }
        public int Language { get; set; }
        public List<ShWeb> Webs { get; set; }
        public List<ShFeature> WebFeatures { get; set; }
        public List<ShTask> CustomTaskTypes { get; set; }
        public List<ShList> Lists { get; set; }
        public List<ShContentFolder> ContentFolders { get; set; }
        public Dictionary<string, string> Quicklaunch { get; set; }
        public Dictionary<string, string> Properties { get; set; }

        public ShWeb()
        {
            Webs = new List<ShWeb>();
            WebFeatures = new List<ShFeature>();
            CustomTaskTypes = new List<ShTask>();
            Lists = new List<ShList>();
            Quicklaunch = new Dictionary<string, string>();
            Properties = new Dictionary<string, string>();
            ContentFolders = new List<ShContentFolder>();
        }
    }
}

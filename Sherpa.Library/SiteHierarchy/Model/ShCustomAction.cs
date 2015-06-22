using Microsoft.SharePoint.Client;

namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShCustomAction
    {
        public string Description { get; set; }
        public string Group { get; set; }
        public string ImageUrl { get; set; }
        public string Location { get; set; }
        public string Name { get; set; }
        public string RegistrationId { get; set; }
        public UserCustomActionRegistrationType RegistrationType { get; set; }
        public BasePermissions Rights { get; set; }
        public string ScriptBlock { get; set; }
        public string ScriptSrc { get; set; }
        public int Sequence { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
    }
}

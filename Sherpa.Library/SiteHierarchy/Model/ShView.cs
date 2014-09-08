namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShView
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public string[] ViewFields { get; set; }
        public string Query { get; set; }
        public uint RowLimit { get; set; }
        public bool DefaultView { get; set; }
        public string JSLink { get; set; }

        public ShView()
        {
            ViewFields = new string[] {};
            RowLimit = 30;
        }
    }
}

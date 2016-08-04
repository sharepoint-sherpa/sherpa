namespace Sherpa.Library.SiteHierarchy.Model
{
    public class ShFieldValue
    {
        public string Name { get; set; }
        public string Value { get; set; }

        public ShFieldValue(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}

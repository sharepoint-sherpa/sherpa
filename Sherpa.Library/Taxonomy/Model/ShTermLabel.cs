using Newtonsoft.Json;

namespace Sherpa.Library.Taxonomy.Model
{
    /// <summary>
    /// Corresponds to https://msdn.microsoft.com/en-us/library/microsoft.sharepoint.taxonomy.label_members.aspx
    /// </summary>
    public class ShTermLabel
    {
        [JsonProperty(Order = 1)]
        public int Language { get; set; }
        [JsonProperty(Order = 2)]
        public string Value { get; set; }
        [JsonProperty(Order = 3)]
        public bool IsDefaultForLanguage { get; set; }
    }
}

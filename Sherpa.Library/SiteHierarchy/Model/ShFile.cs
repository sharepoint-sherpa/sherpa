using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sherpa.Library.SiteHierarchy.Model
{
    [Serializable()]
    [System.Xml.Serialization.XmlRoot("Manifest")]
    public class ShFileCollection
    {
        [XmlArray("Files")]
        [XmlArrayItem("File", typeof(ShFile))]
        public ShFile[] Files { get; set; }
        public ShFile GetFileByName(string fileName) {
            for(int i = 0; i < Files.Length; i++) {
                if(Files[i].Name.Equals(fileName)) {
                    return Files[i];
                }
            }
            return null;
        }
    }

    [Serializable()]
    public class ShFile
    {
        [System.Xml.Serialization.XmlElementAttribute("Name")]
        public string Name { get; set; }
        [XmlArray("WebParts")]
        [XmlArrayItem("WebPart", typeof(ShWebPart))]
        public ShWebPart[] WebParts { get; set; }
    }

    [Serializable()]
    public class ShWebPart
    {
        [System.Xml.Serialization.XmlElementAttribute("WebPartZoneID")]
        public string WebPartZoneID { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("WebPartOrder")]
        public string WebPartOrder { get; set; }
        [System.Xml.Serialization.XmlElementAttribute("Definition")]
        public string Definition { get; set; }
    }
}
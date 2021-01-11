using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot("ArrayOfLspInfo")]
    public class LspInfoArray
    {
        [XmlElement(ElementName = "LspInfo")]
        public LspInfo[] LspInfos { get; }
    } 
    
    
}
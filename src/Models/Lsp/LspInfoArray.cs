using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot("ArrayOfLspInfo")]
    public class LspInfoArray
    {
        public LspInfoArray()
        {
        }

        public LspInfoArray(LspInfo[] lspInfos)
        {
            LspInfos = lspInfos;
        }

        [XmlElement(ElementName = "LspInfo")]
        public LspInfo[] LspInfos { get; set; }
    } 
    
    
}
using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot("ArrayOfLspContact")]
    public class LspContactArray
    {

        [XmlElement(ElementName = "LspContact")]
        public LspContact[] LspContacts  { get; }
    } 
    
    
}
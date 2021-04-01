using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot("ArrayOfLspContact")]
    public class LspContactArray
    {
        public LspContactArray()
        {
        }

        public LspContactArray(List<LspContact> lspContacts)
        {
            LspContacts = lspContacts;
        }

        [XmlElement(ElementName = "LspContact", IsNullable=false)]
        public List<LspContact> LspContacts  { get; set; }
    } 
    
    
}
using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    public class LspInfo
    {
        [XmlElement(ElementName = "IsLA")]
        public bool IsLA { get; }

        [XmlElement(ElementName = "NetworkID")]
        public string NetworkID { get; }
    } 

   
}
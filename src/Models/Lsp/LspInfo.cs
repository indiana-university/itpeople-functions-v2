using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    public class LspInfo
    {
        public LspInfo()
        {
        }

        public LspInfo(string networkID, bool isLA)
        {
            IsLA = isLA;
            NetworkID = networkID;
        }

        [XmlElement(ElementName = "IsLA")]
        public bool IsLA { get; set; }

        [XmlElement(ElementName = "NetworkID")]
        public string NetworkID { get; set; }
    } 

   
}
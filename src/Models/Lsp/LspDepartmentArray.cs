using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot("LspDepartment")]
    public class LspDepartmentArray
    {
        [XmlElement(ElementName = "DeptCodeList")]
        public DeptCodeList[] DeptCodeLists { get; }
        
        [XmlElement(ElementName = "NetworkID")]
        public string NetworkID { get; }
    }
}
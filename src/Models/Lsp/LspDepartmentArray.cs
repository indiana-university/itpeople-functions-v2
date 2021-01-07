using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot("LspDepartment")]
    public record LspDepartmentArray
    {
        [XmlElement(ElementName = "DeptCodeList")]
        public DeptCodeList[] DeptCodeLists { get; }
        
        [XmlElement(ElementName = "NetworkID")]
        public string NetworkID { get; }
    }
}
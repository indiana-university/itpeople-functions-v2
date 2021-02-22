using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot("LspDepartment")]
    public class LspDepartmentArray
    {
        public LspDepartmentArray()
        {
        }

        public LspDepartmentArray(string networkID, List<DeptCodeList> deptCodeLists)
        {
            DeptCodeLists = deptCodeLists;
            NetworkID = networkID;
        }

        [XmlElement(ElementName = "DeptCodeList", IsNullable=false)]
        public List<DeptCodeList> DeptCodeLists { get; set; }
        
        [XmlElement(ElementName = "NetworkID")]
        public string NetworkID { get; set; }
    }
}
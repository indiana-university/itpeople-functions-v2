using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot(ElementName="LspDepartment")]
    public class LspDepartmentArray { 

        [XmlElement(ElementName="DeptCodeList")] 
        public DeptCodeList DeptCodeList { get; set; } 

        [XmlElement(ElementName="NetworkID")] 
        public string NetworkID { get; set; }

        public LspDepartmentArray() {}
        public LspDepartmentArray(string networkID, List<string> deptCodes)
        {
            DeptCodeList = new DeptCodeList(deptCodes);
            NetworkID = networkID;
        }
    }
}
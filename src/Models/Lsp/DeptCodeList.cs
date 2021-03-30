using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    [XmlRoot(ElementName="DeptCodeList")]
    public class DeptCodeList { 

        [XmlElement(ElementName="a", IsNullable=false)]
        public List<string> A { get; set; }

        public DeptCodeList()
        {
            A = new List<string>();
        }

        public DeptCodeList(List<string> values)
        {
            A = values;
        }
    }
}
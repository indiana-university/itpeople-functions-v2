using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    public class DeptCodeList
    {
        public DeptCodeList()
        {
        }

        public DeptCodeList(List<string> values)
        {
            Values = values;
        }

        [XmlElement(ElementName = "a", IsNullable=false)]
        public List<string> Values { get; set; }
    }
    
}
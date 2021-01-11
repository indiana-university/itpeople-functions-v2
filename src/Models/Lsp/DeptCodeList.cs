using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    public class DeptCodeList
    {
        [XmlElement(ElementName = "a")]
        public string[] Values { get; }
    }
    
}
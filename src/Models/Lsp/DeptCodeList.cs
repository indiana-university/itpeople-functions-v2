using System;
using System.Xml.Serialization;

namespace Models
{
    [Serializable]
    public record DeptCodeList
    {
        [XmlElement(ElementName = "a")]
        public string[] Values { get; }
    }
    
}
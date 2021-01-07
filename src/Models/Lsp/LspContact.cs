using System;
using System.Xml.Serialization;
namespace Models
{
    [Serializable]
    public record LspContact
    {
        [XmlElement(ElementName = "Email")] 
        public string  Email { get;  } 
        [XmlElement(ElementName = "FullName")] 
        public string FullName { get;  }
        [XmlElement(ElementName = "GroupInternalEmail")] 
        public string GroupInternalEmail { get;  } 
        [XmlElement(ElementName = "NetworkID")] 
        public string NetworkID { get;  } 
        [XmlElement(ElementName = "Phone")] 
        public string Phone { get;  } 
        [XmlElement(ElementName = "PreferredEmail")] 
        public string PreferredEmail { get;  } 
        [XmlElement(ElementName = "isLspAdmin")] 
        public bool IsLSPAdmin { get;  }
    }
    
}
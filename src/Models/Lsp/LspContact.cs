namespace Models
{
    [Serializable]
    public record LspContact
    {
        [XmlElement("Email")] 
        public string  Email { get;  } 
        [XmlElement("FullName")] 
        public string FullName { get;  }
        [XmlElement("GroupInternalEmail")] 
        public string GroupInternalEmail { get;  } 
        [XmlElement("NetworkID")] 
        public string NetworkID { get;  } 
        [XmlElement("Phone")] 
        public string Phone { get;  } 
        [XmlElement("PreferredEmail")] 
        public string PreferredEmail { get;  } 
        [XmlElement("isLspAdmin")] 
        public bool IsLSPAdmin { get;  }
    }
    
}
using System;
using System.Xml.Serialization;
namespace Models
{
    [Serializable]
    public class LspContact
    {
        public LspContact()
        {
        }

        public LspContact(Person person)
        {
            Email = person.CampusEmail;
            FullName = person.Name;
            GroupInternalEmail = person.Notes;
            NetworkID = person.Netid;
            Phone = person.CampusPhone;
            PreferredEmail = person.CampusEmail;
            IsLSPAdmin = person.IsServiceAdmin;
        }

        [XmlElement(ElementName = "Email")] 
        public string  Email { get; set; } 
        [XmlElement(ElementName = "FullName")] 
        public string FullName { get; set; }
        [XmlElement(ElementName = "GroupInternalEmail")] 
        public string GroupInternalEmail { get; set; } 
        [XmlElement(ElementName = "NetworkID")] 
        public string NetworkID { get; set; } 
        [XmlElement(ElementName = "Phone")] 
        public string Phone { get; set; } 
        [XmlElement(ElementName = "PreferredEmail")] 
        public string PreferredEmail { get; set; } 
        [XmlElement(ElementName = "isLspAdmin")] 
        public bool IsLSPAdmin { get; set; }
    }
    
}
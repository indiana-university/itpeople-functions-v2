namespace Models
{
    [Serializable]
    [XmlRoot("ArrayOfLspContact")]
    public record LspContactArray
    {

        [XmlElement(ElementName = "LspContact")]
        public LspContact[] LspContacts  { get; }
    } 
    
    
}
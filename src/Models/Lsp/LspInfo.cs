namespace Models
{
    [Serializable]
    public record LspInfo
    {
        [XmlElement(ElementName = "IsLA")]
        public bool IsLA { get; }

        [XmlElement(ElementName = "NetworkID")]
        public string NetworkID { get; }
    } 

   
}
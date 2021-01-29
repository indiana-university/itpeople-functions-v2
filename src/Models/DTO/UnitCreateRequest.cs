namespace Models
{
    public class UnitCreateRequest
    {
        /// The name of this Unit.
        public string Name { get; set; }
		/// A description of this Unit
        public string Description { get; set; }
        /// A URL for a website.
        public string Url { get; set; }
        /// Email for contacting this Unit
        public string Email { get; set; }
		/// The Unit Id for the this Unit's parent Unit
		public int ParentId {get; set;}

        public UnitCreateRequest(string name, string description, string url, string email, Unit parent = null)
        {
            Name = name;
            Description = description;
            Url = url;
            Email = email;
            ParentId = parent?.Id ?? 0;
        }
    }
    
}
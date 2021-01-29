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
		public int ParendId {get; set;}
    }
    
}
namespace Models
{
    public class PersonUpdateRequest
    {

        /// The physical location (building, room) of this person.
        public string Location { get; set; }    
        /// A collection of IT-related skills, expertise, or interests posessed by this person.
        public string Expertise { get; set; }
        /// A URL for a profile photo or headshot.
        public string PhotoUrl { get; set; }
        /// A collection of IT-related responsibilites of this person.
        public Responsibilities Responsibilities { get; set; }
    }
    
}
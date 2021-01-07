using System.Text.Json;

namespace Models
{
    /// A person doing or supporting IT work
    public record Person : Entity
    {
        /// The net id (username) of this person.
        public string NetId { get; }
        /// The preferred name of this person.
        public string  Name { get; }
        /// The preferred first name of this person.
        public string NameFirst { get; }
        /// The preferred last name of this person.
        public string NameLast { get; }
        /// The job position of this person as defined by HR. This may be different than the person's title in relation to an IT unit.
        public string Position { get; }
        /// The physical location (building, room) of this person.
        public string Location { get; }
        /// The primary campus with which this person is affiliated.
        public string Campus { get; }
        /// The campus phone number of this person.
        public string CampusPhone { get; }
        /// The campus (work) email address of this person.
        public string CampusEmail { get; }
        /// A collection of IT-related skills, expertise, or interests posessed by this person.
        public string Expertise { get; }
        /// Administrative notes about this person, visible only to IT Admins.
        public string Notes { get; }
        /// A URL for a photograph (headshot) of this person.
        public string PhotoUrl { get; }
        /// A collection of IT-related responsibilites of this person.
        public Responsibilities Responsibilities { get; }
        /// The HR department to which this person belongs.
        public int DepartmentId { get; }
        /// Whether this person is an administrator of the IT People service.
        public bool IsServiceAdmin { get; }

        /// The department in this relationship.
        public Department Department { get; }
    }
    
}
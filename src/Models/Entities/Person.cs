using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Models
{
    /// A person doing or supporting IT work
    public class Person : Entity
    {
        /// The net id (username) of this person.
        [Required] public string NetId { get; set; }
        /// The preferred name of this person.
        [Required] public string  Name { get; set; }
        /// The preferred first name of this person.
        [Required] public string NameFirst { get; set; }
        /// The preferred last name of this person.
        [Required] public string NameLast { get; set; }
        /// The job position of this person as defined by HR. This may be different than the person's title in relation to an IT unit.
        [Required] public string Position { get; set; }
        /// The physical location (building, room) of this person.
        [Required] public string Location { get; set; }
        /// The primary campus with which this person is affiliated.
        [Required] public string Campus { get; set; }
        /// The campus phone number of this person.
        [Required] public string CampusPhone { get; set; }
        /// The campus (work) email address of this person.
        [Required] public string CampusEmail { get; set; }
        /// A collection of IT-related skills, expertise, or interests posessed by this person.
        public string Expertise { get; set; }
        /// Administrative notes about this person, visible only to IT Admins.
        public string Notes { get; set; }
        /// A URL for a photograph (headshot) of this person.
        public string PhotoUrl { get; set; }
        /// A collection of IT-related responsibilites of this person.
        public Responsibilities Responsibilities { get; set; }
        /// Whether this person is an administrator of the IT People service.
        public bool IsServiceAdmin { get; set; }
        /// The HR department to which this person belongs.
        public int? DepartmentId { get; set; }
        /// The department in this relationship.
        public Department Department { get; set; }
    }
    
}
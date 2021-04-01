using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    /// A person doing or supporting IT work
    public class HrPerson : Entity
    {
        
        /// The net id (username) of this person.
        public string Netid { get; set; }
        /// The preferred name of this person.
        public string Name { get; set; }
        /// The preferred first name of this person.
        public string NameFirst { get; set; }
        /// The preferred last name of this person.
        public string NameLast { get; set; }
        /// The job position of this person as defined by HR. This may be different than the person's title in relation to an IT unit.
        public string Position { get; set; }
        /// The primary campus with which this person is affiliated.
        public string Campus { get; set; }
        /// The campus phone number of this person.
        public string CampusPhone { get; set; }
        /// The campus (work) email address of this person.
        public string CampusEmail { get; set; }
        /// The short name of the person's HR department (e.g. UA-VPIT).
        public string HrDepartment { get; set; }
        /// The long name / description of the person's HR department.
        [Column("hr_department_desc")]
        public string HrDepartmentDescription { get; set; }

        /// The MarkedForDelete when updating from HR Profile API
        public bool MarkedForDelete { get; set; }
    
    }
    
}
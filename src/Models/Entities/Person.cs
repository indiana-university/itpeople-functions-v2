using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Models
{
    /// A person doing or supporting IT work
    public class Person : Entity
    {
        /// <summary>The net id (username) of this person.</summary>
        [Required] 
        [JsonProperty("netId")]
        public string Netid { get; set; }
        /// <summary>The preferred name of this person.</summary>
        [Required] public string  Name { get; set; }
        /// <summary>The preferred first name of this person.</summary>
        public string NameFirst { get; set; }
        /// <summary>The preferred last name of this person.</summary>
        public string NameLast { get; set; }
        /// <summary>The job position of this person as defined by HR. This may be different than the person's title in relation to an IT unit.</summary>
        [Required] public string Position { get; set; }
        /// <summary>The physical location (building, room) of this person.</summary>
        [Required] public string Location { get; set; }
        /// <summary>The primary campus with which this person is affiliated.</summary>
        [Required] public string Campus { get; set; }
        /// <summary>The campus phone number of this person.</summary>
        [Required] public string CampusPhone { get; set; }
        /// <summary>The campus (work) email address of this person.</summary>
        [Required] public string CampusEmail { get; set; }
        /// <summary>A collection of IT-related skills, expertise, or interests posessed by this person.</summary>
        public string Expertise { get; set; }
        /// <summary>Administrative notes about this person, visible only to IT Admins.</summary>
        public string Notes { get; set; }
        /// <summary>A URL for a photograph (headshot) of this person.</summary>
        public string PhotoUrl { get; set; }
        /// <summary>A collection of IT-related responsibilites of this person.</summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public Responsibilities Responsibilities { get; set; }
        /// <summary>Whether this person is an administrator of the IT People service.</summary>
        public bool IsServiceAdmin { get; set; }
        /// <summary>The HR department to which this person belongs.</summary>
        public int? DepartmentId { get; set; }

        /// Entity Navigation

        /// <summary>The department in this relationship.</summary>
        public Department Department { get; set; }

        /// <summary>The units of which this person is a member</summary>
        [JsonIgnore]
        public List<UnitMember> UnitMemberships { get; set; }

        [JsonIgnore]
        public static string CsvFileName => $"it-people.csv";
        [JsonIgnore]
        public static string CsvHeader => "Name,NetID,Email,Phone,Campus,Department,Position,Interests";
        [JsonIgnore]
        public string AsCsvRow => $"{this.Name},{this.Netid},{this.CampusEmail},{this.CampusPhone},{this.Campus},{this.Department?.Name},{this.Position}";

    }
}

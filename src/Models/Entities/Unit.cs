using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Unit : Entity
    {
        /// The name of this unit.
        [Required]
        public string Name { get; set; }
        /// A description of this unit.
        public string Description { get; set; }
        /// A URL for the website of this unit.
        public string Url { get; set; }
        /// A contact email for this unit.
        public string Email { get; set; }
        /// The unique ID of the parent unit of this unit.
        public int? ParentId { get; set; } 
        /// The parent unit of this unit
        public Unit Parent { get; set; }

        public Unit() {}
        public Unit(string name, string description, string url, string email, Unit parent = null)
        {
            Name = name;
            Description = description;
            Url = url;
            Email = email;
            ParentId = parent?.Id ?? 0;
        }
    }
    
}
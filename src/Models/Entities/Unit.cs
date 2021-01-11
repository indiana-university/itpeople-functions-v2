using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Unit : Entity
    {
        /// The name of this unit.
        [Required]
        public string Name { get; }
        /// A description of this unit.
        public string Description { get; }
        /// A URL for the website of this unit.
        public string Url { get; }
        /// A contact email for this unit.
        public string Email { get; }
        /// The unique ID of the parent unit of this unit.
        public int? ParentId { get; } 
        /// The parent unit of this unit
        public Unit Parent { get; }
    }
    
}
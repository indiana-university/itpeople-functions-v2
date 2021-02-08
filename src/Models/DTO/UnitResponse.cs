using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class UnitResponse : Entity
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
        
        [NotMapped]
        public UnitResponse Parent { get; set; }

        public UnitResponse() {}
        public UnitResponse(Unit unit)
        {
            Id = unit.Id;
            Name = unit.Name;
            Description = unit.Description;
            Url = unit.Url;
            Email = unit.Email;
            Parent = (unit.Parent == null ? null : new UnitResponse(unit.Parent));
        }
    }
}
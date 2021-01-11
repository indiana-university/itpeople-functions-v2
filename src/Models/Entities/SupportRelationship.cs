using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SupportRelationship : Entity 
    { 
        /// The ID of the unit in this relationship
        [Required]
        public int UnitId { get; }
        /// The ID of the department in this relationship
        [Required]
        public int DepartmentId { get; }
        /// The department in this relationship.
        public Department Department { get; }
        /// The unit in this relationship.
        public Unit Unit { get; }
    }

}
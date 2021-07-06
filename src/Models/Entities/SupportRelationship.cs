using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SupportRelationship : Entity 
    { 
        /// The ID of the unit in this relationship
        [Required]
        public int UnitId { get; set; }
        /// The ID of the department in this relationship
        [Required]
        public int DepartmentId { get; set; }
        /// The department in this relationship.
        public Department Department { get; set; }
        /// The unit in this relationship.
        public Unit Unit { get; set; }

        /// The ID of the support type in this relationship
        public int? SupportTypeId { get; set; }

        /// The support type in this relationship
        public SupportType SupportType { get; set; }
    }

}
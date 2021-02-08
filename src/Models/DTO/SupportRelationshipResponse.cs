using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Models
{
    public class SupportRelationshipResponse : Entity
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
        public UnitResponse Unit { get; set; }
    
		public SupportRelationshipResponse(SupportRelationship sr)
		{
			Id = sr.Id;
			Department = sr.Department;
			Unit = new UnitResponse(sr.Unit);
		}
		
		public static List<SupportRelationshipResponse> ConvertList(List<SupportRelationship> relationships)
		{
			return relationships.Select(sr => new SupportRelationshipResponse(sr)).ToList();
		}
	}
}
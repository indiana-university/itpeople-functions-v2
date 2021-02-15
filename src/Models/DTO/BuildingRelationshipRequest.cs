using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class BuildingRelationshipRequest
    {
        /// The ID of the unit in this relationship
        public int UnitId { get; set; }
        /// The ID of the department in this relationship
        public int BuildingId { get; set; }
	}
}
namespace Models
{
    public class SupportRelationshipRequest
    { 
        /// The ID of the unit in this relationship
        public int  UnitId { get; set; }
        /// The ID of the department in this relationship
        public int  DepartmentId { get; set; }
    }
}
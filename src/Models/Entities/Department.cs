using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class DepartmentResponse : Entity
    {
        /// The name of this department.
        [Required] public string Name { get; set; }
        /// A description or longer name of this department.
        public string Description { get; set; }
        
        public int? PrimarySupportUnitId { get; set; }
        [NotMapped]
        public UnitResponse PrimarySupportUnit { get; set; }

        public DepartmentResponse() {}

        public DepartmentResponse(Department department)
        {
            Id = department.Id;
            Name = department.Name;
            Description = department.Description;
            PrimarySupportUnit = department.PrimarySupportUnit == null
                ? null
                : new UnitResponse(department.PrimarySupportUnit);
        }
    }

    public class Department : DepartmentResponse
    {    
        public new Unit PrimarySupportUnit { get; set; }
    }
}
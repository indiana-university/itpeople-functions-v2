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
        
        public int? ReportSupportingUnitId { get; set; }
        [NotMapped]
        public UnitResponse ReportSupportingUnit { get; set; }

        public DepartmentResponse() {}

        public DepartmentResponse(Department department)
        {
            Id = department.Id;
            Name = department.Name;
            Description = department.Description;
            ReportSupportingUnit = department.ReportSupportingUnit == null
                ? null
                : new UnitResponse(department.ReportSupportingUnit);
        }
    }
    public class Department : DepartmentResponse
    {    
        public new Unit ReportSupportingUnit { get; set; }
    }
}
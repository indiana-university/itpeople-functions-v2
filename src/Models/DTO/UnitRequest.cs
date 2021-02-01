using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class UnitRequest
    {
        /// The name of this unit.
        [Required(ErrorMessage = MalformedRequest)]
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
        private Unit Parent;

        public const string MalformedRequest = "The request body is malformed or missing. The Name field is required.";

        public UnitRequest()
        {
            ParentId = null;
        }

        public Unit GetParent() => Parent;
        public void SetParent(Unit unit) => Parent = unit;
    }
}
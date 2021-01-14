using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Department : Entity
    {    
        /// The name of this department.
        [Required] public string Name { get; set; }
        /// A description or longer name of this department.
        public string Description { get; set; }
    }
}
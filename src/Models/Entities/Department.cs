using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    [Table("departments")]
    public class Department : Entity
    {    
        /// The name of this department.
        [Required] public string Name { get; set; }
        /// A description or longer name of this department.
        public string Description { get; set; }
    }
}
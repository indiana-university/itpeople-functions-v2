using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
    public class Tool : Entity
    {        
        
        /// The name of this tool.
        public string Name { get; set; }
        /// A description of this tool.
        public string Description { get; set; }
        /// A description of this tool.
        
        [Column("ad_path")]
        public string ADPath { get; set; }
        /// Whether this tool is scoped to a department via a unit-department support relationship.
       public bool DepartmentScoped { get; set; }

    }
}
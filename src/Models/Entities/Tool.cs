using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

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

        public IEnumerable<MemberTool> MemberTools { get; set; }

		public ToolResponse ToToolResponse() => new ToolResponse
		{
			Id = Id,
			Name = Name,
			Description = Description,
			ADPath = ADPath,
			DepartmentScoped = DepartmentScoped,
			MemberTools = MemberTools == null ? new List<MemberToolResponse>() : MemberToolResponse.ConvertList(MemberTools.ToList())
		};
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace Models
{
	public class ToolResponse : Entity, IComparable
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

		public IEnumerable<MemberToolResponse> MemberTools { get; set; }

		public static List<ToolResponse> ConvertList(List<Tool> source)
            => source.Select(t => t.ToToolResponse()).ToList();

	}
}
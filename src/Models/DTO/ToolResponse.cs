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

		public int CompareTo(object obj)
		{
			Console.Write("is null"); ;
			if (obj == null) return 1;

			ToolResponse other = obj as ToolResponse;
			Console.Write(other.Id);
			Console.Write(other.Name) ;
			Console.Write("---");
			Console.Write(Id);
			Console.Write(Name);
			Console.Write("xxxxx");
			if (other != null)
			{
				return Id.CompareTo(other.Id);
			}
			else
			{
				throw new ArgumentException($"Object is not a {nameof(ToolResponse)}.");
			}
		}
	}
}
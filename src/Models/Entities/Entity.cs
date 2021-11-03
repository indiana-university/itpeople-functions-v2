using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Entity: IComparable
    {
        /// The unique ID of this class.
        [Required] public int Id { get; set; }

		public int CompareTo(object obj)
		{
			if (obj == null) return 1;
			var other = obj as Entity;
			return Id.CompareTo(other.Id);
		}
	}
}
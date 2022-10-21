using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Models
{
	public class Notification : Entity
	{
		[Required]
		public DateTime Created { get; set; } = DateTime.Now;
		[Required(AllowEmptyStrings = false)]
		public string Message { get; set; }
		public DateTime? Reviewed { get; set; }
		public string Netid { get; set; }
	}
}
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class UnitResponse : Entity
    {
		/// The name of this unit.
		[Required]
        public string Name { get; set; }
        /// A description of this unit.
        public string Description { get; set; }
        /// A URL for the website of this unit.
        public string Url { get; set; }
        /// A contact email for this unit.
        public string Email { get; set; }

		public UnitResponse() {}
		public UnitResponse(Unit unit)
		{
			Id = unit.Id;
			Name = unit.Name;
			Description = unit.Description;
			Url = unit.Url;
			Email = unit.Email;
		}
	}
}
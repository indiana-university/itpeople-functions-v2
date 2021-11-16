using System.Collections.Generic;
using System.Linq;

namespace Models
{
	public class SsspSupportRelationshipResponse
	{
		public int Key { get; set; }
		public string Dept { get; set; }
		public string DeptDescription { get; set; }
		public string ContactEmail { get; set; }

		public static List<SsspSupportRelationshipResponse> FromSupportRelationship(SupportRelationship input)
		{
			var output = new List<SsspSupportRelationshipResponse>();
			// SupportRelationships for Units that don't have an email set should use the emails of their leaders.
			if(string.IsNullOrWhiteSpace(input.Unit.Email))
			{
				foreach(var leader in input.Unit.UnitMembers.Where(um => um.Role == Role.Leader && string.IsNullOrWhiteSpace(um.Person.CampusEmail) == false))
				{
					output.Add(
						new SsspSupportRelationshipResponse
						{
							Key = 0,
							Dept = input.Department.Name,
							DeptDescription = input.Department.Description,
							ContactEmail = leader.Person.CampusEmail
						}
					);
				}
			}
			else
			{
				// If we have it just use the Unit.Email value.
				output.Add(
					new SsspSupportRelationshipResponse
					{
						Key = 0,
						Dept = input.Department.Name,
						DeptDescription = input.Department.Description,
						ContactEmail = input.Unit.Email
					}
				);
			}

			return output;
		}
	}
}
using System.Collections.Generic;
using System.Linq;
using Models.Enums;

namespace Models
{
	public class UnitMember : Entity
    {
        
        /// The ID of the unit class.
        public int UnitId { get; set; }
        /// The role of the person in this membership as part of the unit.
        public Role Role { get; set; }
        /// The permissions of the person in this membership as part of the unit. Defaults to 'viewer'.
        public UnitPermissions Permissions { get; set; }
        /// The ID of the person class. This can be null if the position is vacant.
        public int? PersonId { get; set; }    
        /// The title/position of this membership.
        public string Title { get; set; }   
        /// The percentage of time allocated to this position by this person (in case of split appointments).
        public int Percentage { get; set; }
        /// Notes about this person (for admins/reporting eyes only.)
        public string Notes { get; set; }
        /// The netid of the person related to this membership.       
        public string Netid { get => this.Person.Netid; }
        /// The person related to this membership.
        public Person Person { get; set; }
        /// The unit related to this membership.
        public Unit Unit { get; set; }
        /// The tools that can be used by the person in this position as part of this unit.
        public List<MemberTool> MemberTools { get;  set; }

        public UnitMemberResponse ToUnitMemberResponse(EntityPermissions perms) 
        {
            return new UnitMemberResponse()
            {
                Id = Id,
                Role = Role,
                Permissions = Permissions,
                PersonId = PersonId,
                Title = Title,
                Percentage = Percentage,
                Notes = (perms.HasFlag(EntityPermissions.Post) || perms.HasFlag(EntityPermissions.Put) || perms.HasFlag(EntityPermissions.Delete)) ? Notes : "",
                Person = Person,
                UnitId = UnitId,
                Unit = new UnitResponse(Unit),
                MemberTools = MemberTools.Select(mt => mt.ToMemberToolResponse()).ToList()
            };
        }
    }
}